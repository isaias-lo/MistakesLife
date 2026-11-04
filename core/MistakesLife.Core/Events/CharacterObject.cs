using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Life;
using MistakesLife.Core.Scripting;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Un personaje visto desde el lenguaje de condiciones ("jugador").
    /// Datos: edad, edad_semanas, salud, felicidad, inteligencia, vivo, sexo,
    /// genero, identidad, orientacion,
    /// apertura, responsabilidad, extraversion, amabilidad, neuroticismo,
    /// pais, ciudad, comuna, nivel_hogar, etapa, madre, padre,
    /// estudia, nivel_estudio, institucion, semestres_aprobados, semestres_carrera, titulado,
    /// pareja, hijos, adoptado, dinero, trabaja, tipo_trabajo, sueldo, sueldo_liquido,
    /// declaracion_pendiente, declaracion_saldo, hijos_menores, gastos, meses_en_rojo,
    /// fondo_pension, pension_estimada, meses_cotizados, jubilado, pension, pension_universal,
    /// deuda, cuotas_deudas, moroso, puede_repactar, amigos, amigo, colega, rival, ex.
    /// En otra persona a la que se llegó desde alguien (jugador.madre, jugador.amigo):
    /// carino, confianza, respeto, rencor (lo que siente por ese alguien, 0 a 100, se
    /// pueden cambiar) y vinculo ('amistad', 'colega', 'rival', 'ex' o nada).
    /// Métodos: tiene('marca'), marcar('marca'), desmarcar('marca'),
    /// titulado_en('carrera o nivel'), titularse(), desertar(),
    /// formar_pareja() o formar_pareja('género'), terminar_pareja(), tener_hijo(),
    /// cambiar_genero('género'), cambiar_nombre(), adoptar(),
    /// trabajar(sueldo) o trabajar(sueldo, 'honorarios'), dejar_trabajo(), declarar(),
    /// jubilarse(), recibir_pension_universal(), endeudarse('tipo', monto), deuda_de('tipo'), repactar(),
    /// conocer('amistad' | 'colega' | 'rival'); en otra persona, vincular('lazo') y alejarse().
    /// </summary>
    public sealed class CharacterObject : IScriptObject
    {
        /// <summary>Método para endeudarse: la validación revisa que el tipo de deuda exista en todo país.</summary>
        public const string BorrowMethod = "endeudarse";

        /// <summary>Método que lee lo que se debe de un tipo de deuda.</summary>
        public const string DebtOfMethod = "deuda_de";

        /// <summary>Método que crea a alguien nuevo en la vida del personaje: conocer('amistad').</summary>
        public const string MeetMethod = "conocer";

        /// <summary>Método que cambia el lazo con quien se llegó: jugador.colega.vincular('amistad').</summary>
        public const string BondMethod = "vincular";

        private readonly Character _character;

        /// <summary>
        /// La persona desde la que se llegó a este personaje (en jugador.madre, el
        /// jugador), o null. Los sentimientos (carino, confianza, respeto, rencor)
        /// son lo que este personaje siente por ella.
        /// </summary>
        private readonly Character? _viewer;

        /// <summary>El personaje que muestra este objeto.</summary>
        internal Character Character => _character;

        public CharacterObject(Character character)
            : this(character, null)
        {
        }

        public CharacterObject(Character character, Character? viewer)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _viewer = viewer;
        }

        public Value Get(string member, IScriptScope scope)
        {
            var date = ((EventScope)scope).World.Date;
            switch (member)
            {
                case "edad":
                    return Value.FromNumber(_character.AgeInYears(date));
                case "edad_semanas":
                    return Value.FromNumber(_character.AgeInWeeks(date));
                case "salud":
                    return Value.FromNumber(_character.Stats.Health);
                case "felicidad":
                    return Value.FromNumber(_character.Stats.Happiness);
                case "inteligencia":
                    return Value.FromNumber(_character.Stats.Smarts);
                case "vivo":
                    return Value.FromBool(_character.IsAlive);
                case "sexo":
                    return Value.FromText(_character.Sex == Sex.Female ? "femenino" : "masculino");
                case "genero":
                    return Value.FromText(Diversity.Key(_character.Gender));
                case "identidad":
                    return Value.FromText(Diversity.Key(_character.GenderIdentity));
                case "orientacion":
                    return Value.FromText(Diversity.Key(_character.Orientation));
                case "apertura":
                    return Value.FromNumber(_character.Personality.Openness);
                case "responsabilidad":
                    return Value.FromNumber(_character.Personality.Conscientiousness);
                case "extraversion":
                    return Value.FromNumber(_character.Personality.Extraversion);
                case "amabilidad":
                    return Value.FromNumber(_character.Personality.Agreeableness);
                case "neuroticismo":
                    return Value.FromNumber(_character.Personality.Neuroticism);
                case "pais":
                    return Value.FromText(_character.Residence?.CountryId ?? string.Empty);
                case "ciudad":
                    return Value.FromText(_character.Residence?.CityId ?? string.Empty);
                case "comuna":
                    return Value.FromText(_character.Residence?.CommuneId ?? string.Empty);
                case "nivel_hogar":
                    return Value.FromNumber(_character.HouseholdLevel);
                case "etapa":
                    return Value.FromText(_character.EducationStageId ?? string.Empty);
                case "estudia":
                    return TextOrNull(_character.Studies?.CareerId);
                case "nivel_estudio":
                    return TextOrNull(_character.Studies?.LevelId);
                case "institucion":
                    return TextOrNull(_character.Studies?.Institution);
                case "semestres_aprobados":
                    return Value.FromNumber(_character.Studies?.CompletedSemesters ?? 0);
                case "semestres_carrera":
                    return Value.FromNumber(_character.Studies?.RequiredSemesters ?? 0);
                case "titulado":
                    return Value.FromBool(_character.Degrees.Count > 0);
                case "madre":
                    return Relative(_character.MotherId, scope);
                case "padre":
                    return Relative(_character.FatherId, scope);
                case "pareja":
                    return Relative(_character.PartnerId, scope);
                case "adoptado":
                    return Value.FromBool(_character.Adopted);
                case "dinero":
                    return Value.FromNumber((double)_character.Money);
                case "trabaja":
                    return Value.FromBool(_character.Job != null);
                case "tipo_trabajo":
                    return _character.Job == null ? Value.Null : Value.FromText(JobKindKey(_character.Job.Kind));
                case "sueldo":
                    return Value.FromNumber((double)(_character.Job?.Salary ?? 0));
                case "hijos_menores":
                    return Value.FromNumber(((EventScope)scope).World.ChildrenOf(_character.Id).Count(c => c.IsAlive && c.AgeInYears(date) < 18));
                case "gastos":
                    return Value.FromNumber((double)_character.LastExpenses.Sum(l => l.Amount));
                case "meses_en_rojo":
                    return Value.FromNumber(_character.MonthsInRed);
                case "declaracion_pendiente":
                    return Value.FromBool(_character.PendingTaxReturn != null);
                case "declaracion_saldo":
                    return Value.FromNumber((double)(_character.PendingTaxReturn?.Balance ?? 0));
                case "deuda":
                    return Value.FromNumber((double)_character.Debts.Sum(d => d.Balance));
                case "cuotas_deudas":
                    return Value.FromNumber((double)_character.LastDebtPayment);
                case "moroso":
                    return Value.FromBool(_character.Delinquent);
                case "puede_repactar":
                    return Value.FromBool(Economy.DebtSystem.CanReschedule(_character, Debts(scope)));
                case "meses_cotizados":
                    return Value.FromNumber(_character.ContributedMonths);
                case "fondo_pension":
                    return Value.FromNumber((double)PayrollToday(_character.PensionFund, scope));
                case "pension_estimada":
                    return Value.FromNumber((double)PayrollToday(Economy.PensionSystem.EstimatedPension(_character, Pensions(scope)), scope));
                case "jubilado":
                    return Value.FromBool(_character.Pension.HasValue);
                case "pension":
                    return Value.FromNumber((double)_character.LastPensionPayment);
                case "pension_universal":
                    return Value.FromBool(_character.ReceivesUniversalPension);
                case "sueldo_liquido":
                    return Value.FromNumber(_character.Job == null ? 0 : (double)(_character.LastPayslip?.Net ?? 0));
                case "hijos":
                    return Value.FromNumber(((EventScope)scope).World.ChildrenOf(_character.Id).Count(c => c.IsAlive));
                case "carino":
                case "confianza":
                case "respeto":
                case "rencor":
                    return Value.FromNumber(Feeling(RelationshipRules.Feelings(((EventScope)scope).World, _character, Viewer(member)), member));
                case "vinculo":
                    string? bond = RelationshipRules.BondKey(_character.RelationshipWith(Viewer(member).Id)?.Bond ?? RelationshipBond.None);
                    return TextOrNull(bond);
                case "amigos":
                    return Value.FromNumber(RelationshipRules.WithBond(((EventScope)scope).World, _character, RelationshipBond.Friend).Count);
                case "amigo":
                    return Closest(RelationshipBond.Friend, scope);
                case "colega":
                    return Closest(RelationshipBond.Colleague, scope);
                case "rival":
                    return Closest(RelationshipBond.Rival, scope);
                case "ex":
                    return Closest(RelationshipBond.Ex, scope);
                default:
                    throw new ScriptException($"jugador.{member} no existe.");
            }
        }

        /// <summary>Lleva un monto en dinero del inicio del mundo a precios de hoy del país donde vive.</summary>
        private decimal PayrollToday(decimal amount, IScriptScope scope)
        {
            string? country = _character.Residence?.CountryId;
            Economy.CountryEconomy? economy = country == null ? null : ((EventScope)scope).World.EconomyOf(country);
            return Economy.PayrollCalculator.ToToday(amount, economy?.PriceIndex ?? 1);
        }

        /// <summary>Las reglas de deudas del país donde vive, o null.</summary>
        private Content.DebtsDefinition? Debts(IScriptScope scope)
        {
            string? country = _character.Residence?.CountryId;
            return country == null ? null : ((EventScope)scope).Content.GetCountry(country).Economy?.Debts;
        }

        /// <summary>Las reglas de pensiones del país donde vive, o null.</summary>
        private Content.PensionsDefinition? Pensions(IScriptScope scope)
        {
            string? country = _character.Residence?.CountryId;
            return country == null ? null : ((EventScope)scope).Content.GetCountry(country).Economy?.Pensions;
        }

        private static Value TextOrNull(string? text) => text == null ? Value.Null : Value.FromText(text);

        private Value Relative(int? id, IScriptScope scope)
        {
            if (id.HasValue && ((EventScope)scope).World.TryGetCharacter(id.Value, out Character? relative))
            {
                return Value.FromObject(new CharacterObject(relative, _character));
            }

            return Value.Null;
        }

        private Value Closest(RelationshipBond bond, IScriptScope scope)
        {
            Character? other = RelationshipRules.Closest(((EventScope)scope).World, _character, bond);
            return other == null ? Value.Null : Value.FromObject(new CharacterObject(other, _character));
        }

        /// <summary>La persona por quien se preguntan los sentimientos; error si se llegó directo (jugador.carino).</summary>
        private Character Viewer(string member)
        {
            return _viewer ?? throw new ScriptException(
                $"{member} solo se usa en otra persona a la que se llega desde alguien (es sobre lo que siente por él), por ejemplo jugador.madre.{member} o jugador.amigo.{member}.");
        }

        private static int Feeling(Relationship feelings, string member)
        {
            switch (member)
            {
                case "carino":
                    return feelings.Affection;
                case "confianza":
                    return feelings.Trust;
                case "respeto":
                    return feelings.Respect;
                default:
                    return feelings.Resentment;
            }
        }

        private static RelationshipBond ParseBond(string method, string key)
        {
            if (!RelationshipRules.TryParseBond(key, out RelationshipBond bond))
            {
                throw new ScriptException($"{method}('{key}'): el lazo debe ser 'amistad', 'colega' o 'rival'.");
            }

            return bond;
        }

        public void Set(string member, Value value, IScriptScope scope)
        {
            ((EventScope)scope).RequireWritable("jugador." + member);
            if (member == "dinero")
            {
                // Se guarda en unidades enteras de la moneda (sin centavos).
                _character.Money = Math.Round((decimal)value.AsNumber("jugador.dinero"), MidpointRounding.AwayFromZero);
                return;
            }

            int number = (int)Math.Round(value.AsNumber("jugador." + member), MidpointRounding.AwayFromZero);
            switch (member)
            {
                case "carino":
                case "confianza":
                case "respeto":
                case "rencor":
                    Relationship feelings = RelationshipRules.Edit(((EventScope)scope).World, _character, Viewer(member));
                    if (member == "carino")
                    {
                        feelings.Affection = number;
                    }
                    else if (member == "confianza")
                    {
                        feelings.Trust = number;
                    }
                    else if (member == "respeto")
                    {
                        feelings.Respect = number;
                    }
                    else
                    {
                        feelings.Resentment = number;
                    }

                    break;
                case "salud":
                    _character.Stats.Health = number;
                    break;
                case "felicidad":
                    _character.Stats.Happiness = number;
                    break;
                case "inteligencia":
                    _character.Stats.Smarts = number;
                    break;
                case "semestres_aprobados":
                    if (_character.Studies == null)
                    {
                        throw new ScriptException("jugador.semestres_aprobados solo se puede cambiar mientras estudia.");
                    }

                    _character.Studies.CompletedSemesters = number;
                    break;
                default:
                    throw new ScriptException($"jugador.{member} no se puede cambiar.");
            }
        }

        public Value Call(string method, Value[] args, IScriptScope scope)
        {
            switch (method)
            {
                case "titularse":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.titularse");
                    RequireStudies(method);
                    _character.Graduate();
                    return Value.Null;
                case "desertar":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.desertar");
                    RequireStudies(method);
                    _character.Studies = null;
                    return Value.Null;
                case "formar_pareja":
                    Gender? gender = null;
                    if (args.Length > 0)
                    {
                        string key = EventScope.SingleText("jugador." + method, args);
                        if (!Diversity.TryParseGender(key, out Gender parsed))
                        {
                            throw new ScriptException($"jugador.formar_pareja('{key}'): el género debe ser 'femenino', 'masculino' o 'no_binario'.");
                        }

                        gender = parsed;
                    }

                    ((EventScope)scope).RequireWritable("jugador.formar_pareja");
                    if (_character.PartnerId.HasValue)
                    {
                        throw new ScriptException("jugador.formar_pareja(): ya tiene pareja.");
                    }

                    FamilyFactory.CreatePartner(((EventScope)scope).World, ((EventScope)scope).Content, _character, gender);
                    return Value.Null;
                case "terminar_pareja":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.terminar_pareja");
                    if (!_character.PartnerId.HasValue)
                    {
                        throw new ScriptException("jugador.terminar_pareja(): no tiene pareja.");
                    }

                    FamilyFactory.EndPartnership(((EventScope)scope).World, _character);
                    return Value.Null;
                case "tener_hijo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.tener_hijo");
                    Character child = FamilyFactory.CreateChild(((EventScope)scope).World, ((EventScope)scope).Content, _character);
                    ((EventScope)scope).World.WriteJournal(_character.Id, child.Sex == Sex.Female ? "diario.nace_hija" : "diario.nace_hijo");
                    return Value.Null;
                case "cambiar_genero":
                    string genderKey = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador.cambiar_genero");
                    if (!Diversity.TryParseGender(genderKey, out Gender newGender))
                    {
                        throw new ScriptException($"jugador.cambiar_genero('{genderKey}'): el género debe ser 'femenino', 'masculino' o 'no_binario'.");
                    }

                    _character.Gender = newGender;
                    return Value.Null;
                case "cambiar_nombre":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.cambiar_nombre");
                    FamilyFactory.ChangeFirstName(((EventScope)scope).World, ((EventScope)scope).Content, _character);
                    return Value.Null;
                case "adoptar":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.adoptar");
                    Character adopted = FamilyFactory.Adopt(((EventScope)scope).World, ((EventScope)scope).Content, _character);
                    ((EventScope)scope).World.WriteJournal(_character.Id, adopted.Sex == Sex.Female ? "diario.adopcion_hija" : "diario.adopcion_hijo");
                    return Value.Null;
                case "trabajar":
                    ((EventScope)scope).RequireWritable("jugador.trabajar");
                    StartJob((EventScope)scope, args);
                    return Value.Null;
                case "declarar":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.declarar");
                    if (_character.PendingTaxReturn == null)
                    {
                        throw new ScriptException("jugador.declarar(): no tiene una declaración pendiente (revisa jugador.declaracion_pendiente).");
                    }

                    Economy.TaxSystem.Accept(((EventScope)scope).World, _character);
                    return Value.Null;
                case "jubilarse":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.jubilarse");
                    if (_character.Pension.HasValue)
                    {
                        throw new ScriptException("jugador.jubilarse(): ya está jubilado (revisa jugador.jubilado).");
                    }

                    Economy.PensionSystem.Retire(_character, Pensions(scope));
                    return Value.Null;
                case "recibir_pension_universal":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.recibir_pension_universal");
                    _character.ReceivesUniversalPension = true;
                    return Value.Null;
                case BorrowMethod:
                    ((EventScope)scope).RequireWritable("jugador." + BorrowMethod);
                    Borrow((EventScope)scope, args);
                    return Value.Null;
                case "repactar":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.repactar");
                    Content.DebtsDefinition? debtRules = Debts(scope);
                    if (debtRules == null || Economy.DebtSystem.Reschedule(_character, debtRules) == 0)
                    {
                        throw new ScriptException("jugador.repactar(): no tiene deudas atrasadas que se puedan repactar (revisa jugador.puede_repactar).");
                    }

                    return Value.Null;
                case DebtOfMethod:
                    string debtType = EventScope.SingleText("jugador." + method, args);
                    return Value.FromNumber((double)_character.Debts.Where(d => d.TypeId == debtType).Sum(d => d.Balance));
                case "dejar_trabajo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.dejar_trabajo");
                    _character.Job = null;
                    return Value.Null;
                case MeetMethod:
                    string meetKey = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    FamilyFactory.CreateAcquaintance(((EventScope)scope).World, ((EventScope)scope).Content, _character, ParseBond(method, meetKey));
                    return Value.Null;
                case BondMethod:
                    string bondKey = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    RelationshipRules.SetBond(((EventScope)scope).World, _character, Viewer(method + "()"), ParseBond(method, bondKey));
                    return Value.Null;
                case "alejarse":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.alejarse");
                    RelationshipRules.SetBond(((EventScope)scope).World, _character, Viewer(method + "()"), RelationshipBond.None);
                    return Value.Null;
                case "titulado_en":
                    string career = EventScope.SingleText("jugador." + method, args);
                    return Value.FromBool(_character.Degrees.Any(d => d == career || d.StartsWith(career + ".", StringComparison.Ordinal)));
            }

            string flag = EventScope.SingleText("jugador." + method, args);
            switch (method)
            {
                case "tiene":
                    return Value.FromBool(_character.HasFlag(flag));
                case "marcar":
                    ((EventScope)scope).RequireWritable("jugador.marcar");
                    _character.SetFlag(flag);
                    return Value.Null;
                case "desmarcar":
                    ((EventScope)scope).RequireWritable("jugador.desmarcar");
                    _character.ClearFlag(flag);
                    return Value.Null;
                default:
                    throw new ScriptException($"jugador.{method}() no existe.");
            }
        }

        /// <summary>
        /// endeudarse('tipo', monto): pide prestado ese monto, escrito en dinero del
        /// inicio del mundo, con un tipo de deuda de su país.
        /// </summary>
        private void Borrow(EventScope scope, Value[] args)
        {
            if (args.Length != 2)
            {
                throw new ScriptException("jugador.endeudarse() necesita el tipo de deuda y el monto: endeudarse('deuda.chile.consumo', 1000000).");
            }

            string typeId = args[0].AsText("jugador.endeudarse()");
            double amount = args[1].AsNumber("jugador.endeudarse()");
            if (amount <= 0)
            {
                throw new ScriptException("jugador.endeudarse(): el monto debe ser mayor que 0.");
            }

            string? country = _character.Residence?.CountryId;
            Content.DebtTypeDefinition type = (country == null ? null : scope.Content.GetCountry(country).Economy?.Debts?.Type(typeId))
                ?? throw new ScriptException($"jugador.endeudarse('{typeId}'): esa deuda no existe en el país donde vive.");
            double priceIndex = scope.World.EconomyOf(country!)?.PriceIndex ?? 1;
            Economy.DebtSystem.Borrow(_character, type, Economy.PayrollCalculator.ToToday((decimal)amount, priceIndex), priceIndex);
        }

        /// <summary>Clave del tipo de trabajo en el lenguaje: 'dependiente', 'honorarios' o 'informal'.</summary>
        public static string JobKindKey(Economy.JobKind kind) =>
            kind == Economy.JobKind.Freelance ? "honorarios" : kind == Economy.JobKind.Informal ? "informal" : "dependiente";

        /// <summary>
        /// trabajar(sueldo) o trabajar(sueldo, 'honorarios' o 'informal'): empieza un trabajo con ese
        /// sueldo bruto mensual, escrito en dinero del inicio del mundo (por ejemplo
        /// sueldo_titulo() o sueldo_minimo() * 1.2). Reemplaza el trabajo anterior.
        /// </summary>
        private void StartJob(EventScope scope, Value[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                throw new ScriptException("jugador.trabajar() necesita el sueldo y, si quieres, el tipo: trabajar(sueldo_titulo(), 'honorarios').");
            }

            double salary = args[0].AsNumber("jugador.trabajar()");
            if (salary < 0)
            {
                throw new ScriptException("jugador.trabajar(): el sueldo no puede ser negativo.");
            }

            Economy.JobKind kind = Economy.JobKind.Employee;
            if (args.Length == 2)
            {
                string key = args[1].AsText("jugador.trabajar()");
                if (key == "honorarios")
                {
                    kind = Economy.JobKind.Freelance;
                }
                else if (key == "informal")
                {
                    kind = Economy.JobKind.Informal;
                }
                else if (key != "dependiente")
                {
                    throw new ScriptException($"jugador.trabajar(): el tipo '{key}' no existe; usa 'dependiente', 'honorarios' o 'informal'.");
                }
            }

            double index = _character.Residence == null ? 1 : scope.World.EconomyOf(_character.Residence.CountryId)?.PriceIndex ?? 1;
            decimal baseSalary = Math.Round((decimal)salary, MidpointRounding.AwayFromZero);
            _character.Job = new Economy.Job(kind, baseSalary, index);
        }

        private static void NoArguments(string method, Value[] args)
        {
            if (args.Length != 0)
            {
                throw new ScriptException($"jugador.{method}() no recibe valores.");
            }
        }

        private void RequireStudies(string method)
        {
            if (_character.Studies == null)
            {
                throw new ScriptException($"jugador.{method}(): no está estudiando.");
            }
        }

        public override string ToString() => _character.ToString();
    }
}

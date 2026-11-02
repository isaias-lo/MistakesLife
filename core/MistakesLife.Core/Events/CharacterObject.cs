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
    /// fondo_pension, pension_estimada, jubilado, pension, pension_universal.
    /// Métodos: tiene('marca'), marcar('marca'), desmarcar('marca'),
    /// titulado_en('carrera o nivel'), titularse(), desertar(),
    /// formar_pareja() o formar_pareja('género'), terminar_pareja(), tener_hijo(),
    /// cambiar_genero('género'), cambiar_nombre(), adoptar(),
    /// trabajar(sueldo) o trabajar(sueldo, 'honorarios'), dejar_trabajo(), declarar(),
    /// jubilarse(), recibir_pension_universal().
    /// </summary>
    public sealed class CharacterObject : IScriptObject
    {
        private readonly Character _character;

        public CharacterObject(Character character)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
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

        /// <summary>Las reglas de pensiones del país donde vive, o null.</summary>
        private Content.PensionsDefinition? Pensions(IScriptScope scope)
        {
            string? country = _character.Residence?.CountryId;
            return country == null ? null : ((EventScope)scope).Content.GetCountry(country).Economy?.Pensions;
        }

        private static Value TextOrNull(string? text) => text == null ? Value.Null : Value.FromText(text);

        private static Value Relative(int? id, IScriptScope scope)
        {
            if (id.HasValue && ((EventScope)scope).World.TryGetCharacter(id.Value, out Character? relative))
            {
                return Value.FromObject(new CharacterObject(relative));
            }

            return Value.Null;
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
                case "dejar_trabajo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.dejar_trabajo");
                    _character.Job = null;
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

        /// <summary>Clave del tipo de trabajo en el lenguaje: 'dependiente' o 'honorarios'.</summary>
        public static string JobKindKey(Economy.JobKind kind) => kind == Economy.JobKind.Freelance ? "honorarios" : "dependiente";

        /// <summary>
        /// trabajar(sueldo) o trabajar(sueldo, 'honorarios'): empieza un trabajo con ese
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
                else if (key != "dependiente")
                {
                    throw new ScriptException($"jugador.trabajar(): el tipo '{key}' no existe; usa 'dependiente' u 'honorarios'.");
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

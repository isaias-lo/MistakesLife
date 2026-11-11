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
    /// vivienda, arriendo, meses_contrato, cuentas_incluidas, compra,
    /// fondo_pension, pension_estimada, meses_cotizados, jubilado, pension, pension_universal,
    /// deuda, cuotas_deudas, moroso, puede_repactar, amigos, amigo, colega, rival, ex,
    /// area, cargo, cargo_anterior, ultimo_cargo, meses_en_cargo, desempeno (se puede cambiar),
    /// puede_ascender, caminos_ascenso, en_la_cima, busqueda, busca_en, semanas_buscando,
    /// postulaciones, entrevistas, rechazos, entrevista_pendiente, tiene_oferta, oferta_area,
    /// oferta_negociada, oferta_mejorada, contacto.
    /// En otra persona a la que se llegó desde alguien (jugador.madre, jugador.amigo):
    /// carino, confianza, respeto, rencor (lo que siente por ese alguien, 0 a 100, se
    /// pueden cambiar) y vinculo ('amistad', 'colega', 'rival', 'ex' o nada).
    /// Métodos: tiene('marca'), marcar('marca'), desmarcar('marca'),
    /// titulado_en('carrera o nivel'), titularse(), desertar(),
    /// formar_pareja() o formar_pareja('género'), terminar_pareja(), tener_hijo(),
    /// cambiar_genero('género'), cambiar_nombre(), adoptar(),
    /// trabajar(sueldo) o trabajar(sueldo, 'honorarios'), dejar_trabajo(), declarar(),
    /// arrendar('vivienda.pieza'), renovar_arriendo(), dejar_arriendo(),
    /// jubilarse(), recibir_pension_universal(), endeudarse('tipo', monto), deuda_de('tipo'), repactar(),
    /// conocer('amistad' | 'colega' | 'rival'); en otra persona, vincular('lazo') y alejarse(),
    /// trabajar_en('área') o trabajar_en('área', 'honorarios'), puede_trabajar_en('área'),
    /// ascender() o ascender('cargo'), puede_ascender_a('cargo'), bajar_cargo(), cambiar_de_empresa(),
    /// postular_en('área' o 'cualquier_trabajo'), buscar_trabajo('forma'), puede_buscar_con('forma'),
    /// dar_entrevista(puntos), aceptar_oferta(), negociar_oferta(), rechazar_oferta(), dejar_de_buscar(),
    /// dar_celular(), perder_celular(), instalar_app('app'), desinstalar_app('app'), tiene_app('app'),
    /// crear_correo(), recibir_correo('correo').
    /// </summary>
    public sealed class CharacterObject : IScriptObject
    {
        /// <summary>Método para arrendar: la validación revisa que el tipo de vivienda exista con su precio en toda comuna.</summary>
        public const string RentMethod = "arrendar";

        public const string ChooseGroceriesMethod = "elegir_compra";

        /// <summary>Método para endeudarse: la validación revisa que el tipo de deuda exista en todo país.</summary>
        public const string BorrowMethod = "endeudarse";

        /// <summary>Método que lee lo que se debe de un tipo de deuda.</summary>
        public const string DebtOfMethod = "deuda_de";

        /// <summary>Método que crea a alguien nuevo en la vida del personaje: conocer('amistad').</summary>
        public const string MeetMethod = "conocer";

        /// <summary>Método que cambia el lazo con quien se llegó: jugador.colega.vincular('amistad').</summary>
        public const string BondMethod = "vincular";

        /// <summary>Método que empieza un trabajo en un área de su carrera: trabajar_en('carrera.software.area.qa').</summary>
        public const string WorkInAreaMethod = "trabajar_en";

        /// <summary>Método que pregunta si cumple los requisitos de un área.</summary>
        public const string CanWorkInAreaMethod = "puede_trabajar_en";

        /// <summary>Método que asciende al siguiente cargo, o a uno de ellos: ascender('carrera.software.cargo.cto').</summary>
        public const string PromoteMethod = "ascender";

        /// <summary>Método que pregunta si puede ascender ahora a ese cargo.</summary>
        public const string CanPromoteToMethod = "puede_ascender_a";

        /// <summary>Método que dice qué trabajo busca: postular_en('carrera.software.area.qa') o postular_en('cualquier_trabajo').</summary>
        public const string PostulateMethod = "postular_en";

        /// <summary>Método que elige cómo buscar trabajo: buscar_trabajo('busqueda.forma.portales').</summary>
        public const string SearchJobMethod = "buscar_trabajo";

        /// <summary>Método que pregunta si puede buscar de esa forma en su país (y si tiene un contacto, cuando hace falta).</summary>
        public const string CanSearchWithMethod = "puede_buscar_con";

        /// <summary>Método que instala una app en el celular: la validación revisa que la app exista.</summary>
        public const string InstallAppMethod = "instalar_app";

        /// <summary>Método que desinstala una app del celular.</summary>
        public const string UninstallAppMethod = "desinstalar_app";

        /// <summary>Método que pregunta si tiene esa app en el celular.</summary>
        public const string HasAppMethod = "tiene_app";

        /// <summary>Método que hace llegar un correo a su bandeja: la validación revisa que el correo exista.</summary>
        public const string ReceiveMailMethod = "recibir_correo";

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
                case "vivienda":
                    return TextOrNull(_character.Lease?.TypeId);
                case "arriendo":
                    return Value.FromNumber((double)(_character.Lease?.MonthlyRent ?? 0));
                case "meses_contrato":
                    return Value.FromNumber(_character.Lease?.MonthsLeft ?? 0);
                case "compra":
                    return TextOrNull(_character.GroceryBasketId);
                case "cuentas_incluidas":
                    return Value.FromBool(_character.Lease != null
                        && (Economy.HousingRules.RulesFor(((EventScope)scope).Content, _character)?.Type(_character.Lease.TypeId)?.IncludesBills ?? false));
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
                case "area":
                    return TextOrNull(_character.Job?.Position?.AreaId);
                case "cargo":
                    return TextOrNull(_character.Job?.Position?.PositionId);
                case "cargo_anterior":
                    return _character.Job?.Position == null
                        ? Value.Null
                        : TextOrNull(((EventScope)scope).Content.GetCareerPosition(_character.Job.Position.PositionId).Previous?.Id);
                case "ultimo_cargo":
                    return TextOrNull((_character.Job?.Position ?? _character.LastPosition)?.PositionId);
                case "meses_en_cargo":
                    return Value.FromNumber(_character.Job?.Position == null ? 0 : _character.Job.MonthsInPosition);
                case "desempeno":
                    return Value.FromNumber(_character.Job?.Position == null ? 0 : _character.Job.Performance);
                case "puede_ascender":
                    return Value.FromBool(Promotions(scope).Count > 0);
                case "caminos_ascenso":
                    return Value.FromNumber(Promotions(scope).Count);
                case "empresa_tamano":
                    return TextOrNull(Economy.CompanyRules.SizeOf(((EventScope)scope).World, ((EventScope)scope).Content, _character)?.Id);
                case "empresa_mayor":
                    Content.CompanySizeDefinition? current = Economy.CompanyRules.SizeOf(((EventScope)scope).World, ((EventScope)scope).Content, _character);
                    return current == null ? Value.Null : TextOrNull(Economy.CompanyRules.Definition(((EventScope)scope).Content, _character)?.Bigger(current.Id)?.Id);
                case "empresa_quebro":
                    return Value.FromBool(((EventScope)scope).World.CompanyById(_character.Job?.CompanyId)?.IsOpen == false);
                case "tope_de_empresa":
                    return Value.FromBool(Economy.CompanyRules.AtCompanyTop(((EventScope)scope).World, ((EventScope)scope).Content, _character));
                case "en_la_cima":
                    return Value.FromBool(_character.Job?.Position != null && ((EventScope)scope).Content.GetCareerPosition(_character.Job.Position.PositionId).IsTop);
                case "busqueda":
                    return TextOrNull(_character.JobSearch?.MethodId);
                case "busca_en":
                    return TextOrNull(_character.JobSearch?.Target);
                case "semanas_buscando":
                    return Value.FromNumber(_character.JobSearch?.Weeks ?? 0);
                case "postulaciones":
                    return Value.FromNumber(_character.JobSearch?.Applications ?? 0);
                case "entrevistas":
                    return Value.FromNumber(_character.JobSearch?.Interviews ?? 0);
                case "rechazos":
                    return Value.FromNumber(_character.JobSearch?.Rejections ?? 0);
                case "entrevista_pendiente":
                    return Value.FromBool(_character.JobSearch?.Stage == Economy.JobSearchStage.Interview);
                case "tiene_oferta":
                    return Value.FromBool(_character.JobSearch?.Stage == Economy.JobSearchStage.Offer);
                case "oferta_area":
                    return _character.JobSearch?.Stage == Economy.JobSearchStage.Offer && _character.JobSearch.TargetsArea
                        ? Value.FromText(_character.JobSearch.Target)
                        : Value.Null;
                case "oferta_negociada":
                    return Value.FromBool(_character.JobSearch?.Stage == Economy.JobSearchStage.Offer && _character.JobSearch.Negotiated);
                case "oferta_mejorada":
                    return Value.FromBool(_character.JobSearch?.Stage == Economy.JobSearchStage.Offer && _character.JobSearch.Raise > 0);
                case "contacto":
                    return Relative(_character.JobSearch?.ContactId, scope);
                case "tiene_celular":
                    return Value.FromBool(_character.Phone != null);
                case "tiene_correo":
                    return Value.FromBool(_character.Mail != null);
                case "correos":
                    return Value.FromNumber(_character.Mail?.Messages.Count ?? 0);
                case "correos_sin_leer":
                    return Value.FromNumber(_character.Mail?.UnreadCount ?? 0);
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
                case "desempeno":
                    if (_character.Job?.Position == null)
                    {
                        throw new ScriptException("jugador.desempeno solo se puede cambiar en un trabajo de su carrera (revisa jugador.area != nada).");
                    }

                    _character.Job.Performance = number;
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
                case RentMethod:
                    string housingType = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Economy.HousingRules.Sign(((EventScope)scope).World, ((EventScope)scope).Content, _character, housingType);
                    return Value.Null;
                case ChooseGroceriesMethod:
                    string basket = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Economy.GroceryRules.Choose(((EventScope)scope).Content, _character, basket);
                    return Value.Null;
                case "renovar_arriendo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.renovar_arriendo");
                    Economy.HousingRules.Renew(_character, Economy.HousingRules.RulesFor(((EventScope)scope).Content, _character)
                        ?? throw new ScriptException("jugador.renovar_arriendo(): en su país no se arrienda."));
                    return Value.Null;
                case "dejar_arriendo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.dejar_arriendo");
                    Economy.HousingRules.Leave(_character);
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
                case WorkInAreaMethod:
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    WorkInArea((EventScope)scope, args);
                    return Value.Null;
                case CanWorkInAreaMethod:
                    return Value.FromBool(Economy.CareerLadder.CanEnter(((EventScope)scope).Content, _character, Area(scope, EventScope.SingleText("jugador." + method, args)), scope));
                case PromoteMethod:
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Promote((EventScope)scope, args);
                    return Value.Null;
                case CanPromoteToMethod:
                    string target = EventScope.SingleText("jugador." + method, args);
                    return Value.FromBool(Promotions(scope).Any(p => p.Id == target));
                case "cambiar_de_empresa":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.cambiar_de_empresa");
                    ChangeCompany((EventScope)scope);
                    return Value.Null;
                case "bajar_cargo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.bajar_cargo");
                    Demote((EventScope)scope);
                    return Value.Null;
                case PostulateMethod:
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Postulate((EventScope)scope, args);
                    return Value.Null;
                case SearchJobMethod:
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    SearchJob((EventScope)scope, EventScope.SingleText("jugador." + method, args));
                    return Value.Null;
                case CanSearchWithMethod:
                    return Value.FromBool(CanSearchWith((EventScope)scope, EventScope.SingleText("jugador." + method, args)));
                case "dar_entrevista":
                    if (args.Length != 1)
                    {
                        throw new ScriptException("jugador.dar_entrevista() necesita los puntos de cómo le fue: dar_entrevista(10).");
                    }

                    ((EventScope)scope).RequireWritable("jugador.dar_entrevista");
                    if (_character.JobSearch?.Stage != Economy.JobSearchStage.Interview)
                    {
                        throw new ScriptException("jugador.dar_entrevista(): no tiene una entrevista pendiente (revisa jugador.entrevista_pendiente).");
                    }

                    Economy.JobSearchRules.Interview(((EventScope)scope).World, ((EventScope)scope).Content, _character, args[0].AsNumber("jugador.dar_entrevista()"));
                    return Value.Null;
                case "aceptar_oferta":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.aceptar_oferta");
                    AcceptOffer((EventScope)scope);
                    return Value.Null;
                case "negociar_oferta":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.negociar_oferta");
                    RequireOffer(method);
                    Economy.JobSearchRules.Negotiate(((EventScope)scope).World, ((EventScope)scope).Content, _character);
                    return Value.Null;
                case "rechazar_oferta":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.rechazar_oferta");
                    RequireOffer(method);
                    Economy.JobSearchRules.Reject(_character);
                    return Value.Null;
                case "dejar_de_buscar":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.dejar_de_buscar");
                    _character.JobSearch = null;
                    return Value.Null;
                case "dar_celular":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.dar_celular");
                    Devices.PhoneRules.GivePhone(((EventScope)scope).World, ((EventScope)scope).Content, _character);
                    return Value.Null;
                case "perder_celular":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.perder_celular");
                    Devices.PhoneRules.LosePhone(_character);
                    return Value.Null;
                case InstallAppMethod:
                case UninstallAppMethod:
                    string appId = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    if (((EventScope)scope).Content.App(appId) == null)
                    {
                        throw new ScriptException($"jugador.{method}('{appId}'): esa app no existe.");
                    }

                    Devices.Phone phone = _character.Phone
                        ?? throw new ScriptException($"jugador.{method}('{appId}'): no tiene celular (revisa jugador.tiene_celular).");
                    if (method == InstallAppMethod)
                    {
                        phone.Install(appId);
                    }
                    else
                    {
                        phone.Uninstall(appId);
                    }

                    return Value.Null;
                case HasAppMethod:
                    return Value.FromBool(_character.Phone?.Has(EventScope.SingleText("jugador." + method, args)) ?? false);
                case "crear_correo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.crear_correo");
                    Devices.PhoneRules.CreateMail(((EventScope)scope).World, _character);
                    return Value.Null;
                case ReceiveMailMethod:
                    string mailId = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    if (((EventScope)scope).Content.Mail(mailId) == null)
                    {
                        throw new ScriptException($"jugador.{method}('{mailId}'): ese correo no existe.");
                    }

                    Devices.PhoneRules.Receive(((EventScope)scope).World, _character, mailId);
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

            Economy.JobKind kind = args.Length == 2 ? ParseJobKind("jugador.trabajar()", args[1].AsText("jugador.trabajar()")) : Economy.JobKind.Employee;
            decimal baseSalary = Math.Round((decimal)salary, MidpointRounding.AwayFromZero);
            _character.Job = new Economy.Job(kind, baseSalary, PriceIndex(scope));
        }

        /// <summary>
        /// postular_en('área' o 'cualquier_trabajo'), o postular_en('área', 'honorarios'):
        /// dice qué trabajo va a buscar (y con qué contrato, en un área). Para un área
        /// tiene que cumplir sus requisitos. Si ya buscaba, cambia lo que busca y sigue
        /// con lo que llevaba.
        /// </summary>
        private void Postulate(EventScope scope, Value[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                throw new ScriptException("jugador.postular_en() necesita el área (o 'cualquier_trabajo') y, si quieres, el tipo: postular_en('carrera.psicologia.area.clinica', 'honorarios').");
            }

            string target = args[0].AsText("jugador.postular_en()");
            Economy.JobKind kind = args.Length == 2 ? ParseJobKind("jugador.postular_en()", args[1].AsText("jugador.postular_en()")) : Economy.JobKind.Employee;
            if (target != Economy.JobSearch.AnyJob)
            {
                Content.CareerAreaDefinition area = Area(scope, target);
                if (!Economy.CareerLadder.CanEnter(scope.Content, _character, area, scope))
                {
                    throw new ScriptException($"jugador.postular_en('{target}'): no cumple los requisitos de esa área (revisa jugador.puede_trabajar_en('{target}')).");
                }
            }

            if (_character.JobSearch == null)
            {
                _character.JobSearch = new Economy.JobSearch(target);
            }
            else
            {
                _character.JobSearch.Target = target;
            }

            _character.JobSearch.Kind = kind;
        }

        /// <summary>
        /// buscar_trabajo('forma'): empieza a buscar de esa forma (si no dijo qué busca,
        /// busca cualquier trabajo). Con una forma que necesita contacto, ese conocido
        /// queda como quien lo recomienda (jugador.contacto).
        /// </summary>
        private void SearchJob(EventScope scope, string methodId)
        {
            Content.JobSearchMethodDefinition method = Economy.JobSearchRules.Definition(scope.Content, _character)?.Method(methodId)
                ?? throw new ScriptException($"jugador.buscar_trabajo('{methodId}'): esa forma de buscar no existe en el país donde vive (revisa jugador.puede_buscar_con('{methodId}')).");
            int? contactId = null;
            if (method.NeedsContact)
            {
                Character contact = Contact(scope)
                    ?? throw new ScriptException($"jugador.buscar_trabajo('{methodId}'): no tiene un conocido que lo recomiende (revisa jugador.puede_buscar_con('{methodId}')).");
                contactId = contact.Id;
            }

            Economy.JobSearch search = _character.JobSearch ?? new Economy.JobSearch(Economy.JobSearch.AnyJob);
            search.MethodId = method.Id;
            search.ContactId = contactId;
            _character.JobSearch = search;
        }

        private bool CanSearchWith(EventScope scope, string methodId)
        {
            Content.JobSearchMethodDefinition? method = Economy.JobSearchRules.Definition(scope.Content, _character)?.Method(methodId);
            return method != null && (!method.NeedsContact || Contact(scope) != null);
        }

        private Character? Contact(EventScope scope)
        {
            Content.JobSearchDefinition? rules = Economy.JobSearchRules.Definition(scope.Content, _character);
            return rules == null ? null : Economy.JobSearchRules.FindContact(scope.World, _character, rules.ContactAffection);
        }

        private void RequireOffer(string method)
        {
            if (_character.JobSearch?.Stage != Economy.JobSearchStage.Offer)
            {
                throw new ScriptException($"jugador.{method}(): no tiene una oferta de trabajo (revisa jugador.tiene_oferta).");
            }
        }

        /// <summary>
        /// aceptar_oferta(): entra a trabajar con contrato. Si buscaba en un área de su
        /// carrera, en esa área (empresa, cargo y sueldo como trabajar_en); si no, con el
        /// sueldo de <see cref="Economy.JobSearchRules.GeneralSalary"/>. Lo que haya
        /// conseguido al negociar se suma al sueldo. Deja de buscar.
        /// </summary>
        private void AcceptOffer(EventScope scope)
        {
            RequireOffer("aceptar_oferta");
            Economy.JobSearch search = _character.JobSearch!;
            decimal raise = 1 + (decimal)search.Raise;
            if (search.TargetsArea)
            {
                Content.CareerAreaDefinition area = Area(scope, search.Target);
                Content.CareerDefinition career = scope.Content.GetCareer(area.CareerId);
                Economy.Company? company = Economy.CompanyRules.Hire(scope.World, scope.Content, _character, career);
                Content.CareerPositionDefinition position = Economy.CareerLadder.StartingPosition(scope.Content, _character, area);
                Place(scope, search.Kind, area, position, company, raise);
                if (career.WorkFlag != null)
                {
                    _character.SetFlag(career.WorkFlag);
                }
            }
            else
            {
                Content.JobSearchDefinition rules = Economy.JobSearchRules.Definition(scope.Content, _character) ?? new Content.JobSearchDefinition();
                Content.SalaryDefinition? salaries = _character.Residence == null ? null : scope.Content.GetCountry(_character.Residence.CountryId).Economy?.Salaries;
                decimal salary = salaries == null ? 0 : Economy.JobSearchRules.GeneralSalary(rules, salaries, _character, scope.DegreeSalaryOf(_character));
                _character.Job = new Economy.Job(Economy.JobKind.Employee, Math.Round(salary * raise, MidpointRounding.AwayFromZero), PriceIndex(scope));
            }

            _character.JobSearch = null;
            _character.ClearFlag(Economy.JobSearchRules.SearchingFlag);
            _character.SetFlag(Economy.JobSearchRules.WorkingFlag);
            scope.World.WriteJournal(_character.Id, Economy.JobSearchRules.HiredJournal);
        }

        private static Content.CareerAreaDefinition Area(IScriptScope scope, string areaId)
        {
            if (!((EventScope)scope).Content.TryGetCareerArea(areaId, out Content.CareerAreaDefinition? area))
            {
                throw new ScriptException($"El área de carrera '{areaId}' no existe.");
            }

            return area!;
        }

        /// <summary>Los ascensos posibles, solo a cargos que existen en su empresa.</summary>
        private System.Collections.Generic.IReadOnlyList<Content.CareerPositionDefinition> Promotions(IScriptScope scope)
        {
            if (_character.Job == null)
            {
                return Array.Empty<Content.CareerPositionDefinition>();
            }

            int? max = Economy.CompanyRules.MaxDepth(((EventScope)scope).World, ((EventScope)scope).Content, _character);
            return Economy.CareerLadder.EligiblePromotions(((EventScope)scope).Content, _character.Job, scope)
                .Where(p => !max.HasValue || p.Depth <= max.Value)
                .ToList();
        }

        private double PriceIndex(EventScope scope) =>
            _character.Residence == null ? 1 : scope.World.EconomyOf(_character.Residence.CountryId)?.PriceIndex ?? 1;

        /// <summary>
        /// trabajar_en('área') o trabajar_en('área', 'honorarios' o 'informal'): empieza un
        /// trabajo en esa área de su carrera, en el cargo que le da su experiencia
        /// (ver CareerLadder.StartingPosition) y con el sueldo de ese cargo.
        /// </summary>
        private void WorkInArea(EventScope scope, Value[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                throw new ScriptException("jugador.trabajar_en() necesita el área y, si quieres, el tipo: trabajar_en('carrera.software.area.qa', 'honorarios').");
            }

            Content.CareerAreaDefinition area = Area(scope, args[0].AsText("jugador.trabajar_en()"));
            if (!Economy.CareerLadder.CanEnter(scope.Content, _character, area, scope))
            {
                throw new ScriptException($"jugador.trabajar_en('{area.Id}'): no tiene un título de esa carrera o no cumple los requisitos del área (revisa jugador.puede_trabajar_en('{area.Id}')).");
            }

            Economy.JobKind kind = args.Length == 2 ? ParseJobKind("jugador.trabajar_en()", args[1].AsText("jugador.trabajar_en()")) : Economy.JobKind.Employee;
            // En la misma área (por ejemplo, para cambiar de contrato) sigue en su empresa.
            Economy.Company? company = _character.Job?.Position?.AreaId == area.Id && scope.World.CompanyById(_character.Job.CompanyId)?.IsOpen == true
                ? scope.World.CompanyById(_character.Job.CompanyId)
                : Economy.CompanyRules.Hire(scope.World, scope.Content, _character, scope.Content.GetCareer(area.CareerId));
            Content.CareerPositionDefinition position = Economy.CareerLadder.StartingPosition(scope.Content, _character, area);
            Place(scope, kind, area, position, company);
        }

        /// <summary>
        /// cambiar_de_empresa(): se va a una empresa más grande de su carrera si existe
        /// (si no, a otra del mismo tamaño), en la misma área y en su cargo, o en el más
        /// alto que exista allá. El sueldo es el de la nueva empresa.
        /// </summary>
        private void ChangeCompany(EventScope scope)
        {
            Economy.Job? job = _character.Job;
            Content.CompanySizeDefinition? size = Economy.CompanyRules.SizeOf(scope.World, scope.Content, _character);
            if (job?.Position == null || size == null)
            {
                throw new ScriptException("jugador.cambiar_de_empresa(): no trabaja en una empresa de su carrera (revisa jugador.empresa_tamano != nada).");
            }

            Content.CareerAreaDefinition area = scope.Content.GetCareerArea(job.Position.AreaId);
            string target = Economy.CompanyRules.Definition(scope.Content, _character)?.Bigger(size.Id)?.Id ?? size.Id;
            Economy.Company? company = Economy.CompanyRules.Hire(scope.World, scope.Content, _character, scope.Content.GetCareer(area.CareerId), target, job.CompanyId);
            Place(scope, job.Kind, area, scope.Content.GetCareerPosition(job.Position.PositionId), company);
        }

        /// <summary>Lo pone en el cargo de la empresa donde ya trabaja (ascensos y bajadas).</summary>
        private void Place(EventScope scope, Economy.JobKind kind, Content.CareerAreaDefinition area, Content.CareerPositionDefinition position) =>
            Place(scope, kind, area, position, scope.World.CompanyById(_character.Job?.CompanyId));

        /// <summary>
        /// Lo pone en ese cargo en esa empresa: si la empresa es chica y el cargo no existe
        /// ahí, en el más alto que exista; el sueldo es el del cargo por lo que paga la empresa.
        /// </summary>
        private void Place(EventScope scope, Economy.JobKind kind, Content.CareerAreaDefinition area, Content.CareerPositionDefinition position, Economy.Company? company) =>
            Place(scope, kind, area, position, company, 1);

        /// <summary>Igual, con el sueldo multiplicado por <paramref name="raise"/> (lo conseguido al negociar).</summary>
        private void Place(EventScope scope, Economy.JobKind kind, Content.CareerAreaDefinition area, Content.CareerPositionDefinition position, Economy.Company? company, decimal raise)
        {
            Content.CompanySizeDefinition? size = company == null ? null : scope.Content.GetCountry(company.CountryId).Economy?.Companies?.Size(company.SizeId);
            position = Economy.CompanyRules.Cap(position, size?.MaxPositionDepth);
            decimal salary = Economy.CareerLadder.Salary(scope.DegreeSalaryOf(_character), area, position);
            if (size != null)
            {
                salary = Math.Round(salary * (decimal)size.SalaryRelative, MidpointRounding.AwayFromZero);
            }

            salary = Math.Round(salary * raise, MidpointRounding.AwayFromZero);

            Economy.Job job = Economy.CareerLadder.Place(_character, kind, salary, PriceIndex(scope), area, position);
            job.CompanyId = company?.Id;
        }

        /// <summary>ascender() sube al único cargo posible; ascender('cargo') elige uno cuando hay dos caminos.</summary>
        private void Promote(EventScope scope, Value[] args)
        {
            System.Collections.Generic.IReadOnlyList<Content.CareerPositionDefinition> options = Promotions(scope);
            Content.CareerPositionDefinition? target;
            if (args.Length == 0)
            {
                if (options.Count > 1)
                {
                    throw new ScriptException("jugador.ascender(): hay más de un camino; elige uno con ascender('cargo').");
                }

                target = options.FirstOrDefault();
            }
            else
            {
                string id = EventScope.SingleText("jugador.ascender", args);
                target = options.FirstOrDefault(p => p.Id == id);
            }

            if (target == null)
            {
                throw new ScriptException("jugador.ascender(): no puede ascender ahora (revisa jugador.puede_ascender o jugador.puede_ascender_a('cargo')).");
            }

            Economy.Job job = _character.Job!;
            Place(scope, job.Kind, scope.Content.GetCareerArea(job.Position!.AreaId), target);
        }

        private void Demote(EventScope scope)
        {
            Economy.Job? job = _character.Job;
            Content.CareerPositionDefinition? previous = job?.Position == null ? null : scope.Content.GetCareerPosition(job.Position.PositionId).Previous;
            if (previous == null)
            {
                throw new ScriptException("jugador.bajar_cargo(): no hay un cargo más abajo (revisa jugador.cargo_anterior != nada).");
            }

            Place(scope, job!.Kind, scope.Content.GetCareerArea(job.Position!.AreaId), previous);
        }

        private static Economy.JobKind ParseJobKind(string where, string key)
        {
            switch (key)
            {
                case "dependiente":
                    return Economy.JobKind.Employee;
                case "honorarios":
                    return Economy.JobKind.Freelance;
                case "informal":
                    return Economy.JobKind.Informal;
                default:
                    throw new ScriptException($"{where}: el tipo '{key}' no existe; usa 'dependiente', 'honorarios' o 'informal'.");
            }
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

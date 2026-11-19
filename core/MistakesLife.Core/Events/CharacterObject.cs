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
    /// declaracion_pendiente, declaracion_saldo, hijos_menores, hijos_en_casa, hijos_con_otro,
    /// hijos_a_cargo, hijos_por_acordar, coprogenitor, gastos, meses_en_rojo, meses_en_trabajo,
    /// ultimo_sueldo, meses_ultimo_trabajo, tipo_ultimo_trabajo, herencia_pendiente,
    /// vivienda, arriendo, meses_contrato, cuentas_incluidas, compra,
    /// fondo_pension, pension_estimada, meses_cotizados, jubilado, pension, pension_universal,
    /// deuda, cuotas_deudas, moroso, puede_repactar, amigos, amigo, colega, rival, ex,
    /// area, cargo, cargo_anterior, ultimo_cargo, ultima_area, meses_en_cargo, desempeno (se puede cambiar),
    /// oficio, meta, meses_bajo_meta y amonestaciones (se pueden cambiar), pago_variable, en_desafio,
    /// puede_ascender, caminos_ascenso, en_la_cima, busqueda, busca_en, semanas_buscando,
    /// postulaciones, entrevistas, rechazos, entrevista_pendiente, tiene_oferta, oferta_area,
    /// oferta_negociada, oferta_mejorada, contacto, mensajes_sin_leer,
    /// dias_vacaciones (días de vacaciones ganados y no tomados en su trabajo),
    /// pago_vacaciones (lo que le pagarían por ellos al salir; de su último trabajo si ya no trabaja),
    /// renuncio (si ya avisó que se va), semanas_para_irse (cuántas semanas le quedan del aviso),
    /// tiene_negocio, negocio_formal, meses_negocio, ganancia_negocio (su parte del último mes) y
    /// ganado_negocio (todo lo que ha recibido) de su negocio con un socio,
    /// tiene_equipo (si trabaja con su equipo armado: jefe y colegas),
    /// comparte_arriendo (si alguien se vino a vivir con él y paga su parte),
    /// prevision, prevision_privada, tramo_salud, precio_plan_salud, semanas_desde_atencion.
    /// En otra persona a la que se llegó desde alguien (jugador.madre, jugador.amigo):
    /// carino, confianza, respeto, rencor (lo que siente por ese alguien, 0 a 100, se
    /// pueden cambiar) y vinculo ('amistad', 'colega', 'rival', 'ex' o nada), en_tu_equipo (si
    /// trabaja en el equipo de ese alguien), recomendado (si entró a ese equipo recomendado por él)
    /// vive_contigo (si comparte el arriendo de ese alguien), es_tu_socio (si es su socio en un negocio)
    /// y quiere_amistad (si hay química para una amistad con ese alguien: ver SchoolRules.ReadyForFriendship).
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
    /// nuevo_contrato() (mismo empleador y mismo equipo, antigüedad desde cero: la obra siguiente),
    /// pedir_aumento(porcentaje) (con su jefe: puede recibir todo, la mitad o nada; ver Work.RaiseRules),
    /// abrir_negocio(aporte, 'formal' | 'informal') (con la persona del evento como socio; el aporte en la moneda de hoy),
    /// cerrar_negocio(),
    /// recomendar() (recomienda en su equipo a la persona del evento; ver Work.ReferralRules),
    /// compartir_arriendo() (la persona del evento se viene a vivir con él y paga su parte), dejar_de_compartir(),
    /// avisar_renuncia(semanas) (renuncia con aviso: se va en esas semanas),
    /// tomar_vacaciones(días), cobrar_vacaciones() (recibe el pago de los días no tomados y quedan en cero),
    /// desafio('marca') (en un oficio: el desafío de este mes; si cierra el mes sobre el 118 %, gana la marca),
    /// postular_en('área' o 'cualquier_trabajo'), buscar_trabajo('forma'), puede_buscar_con('forma'),
    /// dar_entrevista(puntos), aceptar_oferta(), negociar_oferta(), rechazar_oferta(), dejar_de_buscar(),
    /// dar_celular(), perder_celular(), instalar_app('app'), desinstalar_app('app'), tiene_app('app'),
    /// crear_correo(), recibir_correo('correo'),
    /// afiliarse('previsión'), puede_atenderse('atención'), atenderse('atención'),
    /// hijos_en_casa_entre(desde, hasta) (hijos menores en su casa con esa edad, en años cumplidos).
    /// </summary>
    public sealed class CharacterObject : IScriptObject
    {
        /// <summary>Método para arrendar: la validación revisa que el tipo de vivienda exista con su precio en toda comuna.</summary>
        public const string RentMethod = "arrendar";

        public const string ChooseGroceriesMethod = "elegir_compra";

        public const string ContractMethod = "contratar";

        public const string ServiceMethod = "servicio";

        public const string CancelServiceMethod = "cancelar_servicio";

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

        /// <summary>Método que une ante la ley a la persona con su pareja: la validación revisa que algún país tenga esa unión.</summary>
        public const string UniteMethod = "unirse";

        /// <summary>Método para cambiarse de previsión de salud: la validación revisa que exista en el país.</summary>
        public const string JoinHealthMethod = "afiliarse";

        /// <summary>Método que pregunta si su previsión le deja atenderse ahí.</summary>
        public const string CanAttendMethod = "puede_atenderse";

        /// <summary>Método que paga una atención de salud: la validación revisa que exista en el país.</summary>
        public const string AttendMethod = "atenderse";

        /// <summary>Lo que vale jugador.semanas_desde_atencion si nunca se ha atendido.</summary>
        public const int NeverAttendedWeeks = 9999;

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
                case "energia":
                    return Value.FromNumber(_character.Stats.Energy);
                case "apariencia":
                    return Value.FromNumber(_character.Stats.Looks);
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
                case "en_clases":
                    return Value.FromBool(Education.SchoolRules.InClasses(((EventScope)scope).World, ((EventScope)scope).Content, _character));
                case "curso":
                    return Value.FromNumber(_character.SchoolClass?.Level ?? 0);
                case "en_la_u":
                    return Value.FromBool(Education.UniversityRules.InSection(_character));
                case "en_la_pega":
                    return Value.FromBool(Work.WorkRules.InTeam(_character));
                case "companeros":
                    return Value.FromNumber(Education.SchoolRules.Classmates(((EventScope)scope).World, _character).Count);
                case "prueba_anunciada":
                    return Value.FromBool(_character.SchoolRecord?.TestPending ?? false);
                case "prueba_devuelta":
                    return Value.FromBool(_character.SchoolRecord is Education.SchoolRecord returned && returned.TestMark.HasValue && returned.TestReturnedWeek == date.TotalWeeks);
                case "nota_prueba":
                    return Value.FromNumber(_character.SchoolRecord?.TestMark is double testMark && Scale(scope) is Content.GradeScaleDefinition testScale
                        ? Education.GradeRules.OnSevenScale(testMark, testScale) : 0);
                case "prueba_aprobada":
                    return Value.FromBool(_character.SchoolRecord?.TestMark is double passMark && Scale(scope) is Content.GradeScaleDefinition passScale && passMark >= passScale.Pass);
                case "promedio":
                    return Value.FromNumber(Education.GradeRules.CurrentAverageOnSeven(_character, Scale(scope)));
                case "rojos":
                    return Value.FromNumber(Education.GradeRules.CurrentFailed(_character, Scale(scope)));
                case "repitio":
                    return Value.FromNumber(_character.SchoolRecord?.RepeatedYears ?? 0);
                case "estudia":
                    return TextOrNull(_character.Studies?.CareerId);
                case "nivel_estudio":
                    return TextOrNull(_character.Studies?.LevelId);
                case "institucion":
                    return TextOrNull(_character.Studies?.Institution);
                case "semestres_aprobados":
                    return Value.FromNumber(_character.Studies?.CompletedSemesters ?? 0);
                case "semestres_cursados":
                    return Value.FromNumber(_character.Studies?.SemestersTaken ?? 0);
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
                case "union":
                    return LivingSpouse(scope) == null ? Value.Null : Value.FromText(_character.UnionTypeId!);
                case "conyuge":
                    Character? spouse = LivingSpouse(scope);
                    return spouse == null ? Value.Null : Value.FromObject(new CharacterObject(spouse, _character));
                case "separado":
                    // Unido ante la ley con alguien que ya no es su pareja (separado de hecho).
                    return Value.FromBool(LivingSpouse(scope) != null && _character.PartnerId != _character.SpouseId);
                case "viudo":
                    return Value.FromBool(_character.SpouseId.HasValue && ((EventScope)scope).World.TryGetCharacter(_character.SpouseId.Value, out Character? late) && !late.IsAlive);
                case "adoptado":
                    return Value.FromBool(_character.Adopted);
                case "dinero":
                    return Value.FromNumber((double)_character.Money);
                case "trabaja":
                    return Value.FromBool(_character.Job != null);
                case "meses_en_trabajo":
                    return Value.FromNumber(_character.Job?.MonthsInJob ?? 0);
                case "ultimo_sueldo":
                    return Value.FromNumber((double)(_character.LastJob?.Salary ?? 0));
                case "meses_ultimo_trabajo":
                    return Value.FromNumber(_character.LastJob?.MonthsInJob ?? 0);
                case "tipo_ultimo_trabajo":
                    return _character.LastJob == null ? Value.Null : Value.FromText(JobKindKey(_character.LastJob.Kind));
                case "herencia_pendiente":
                    return Value.FromNumber((double)PayrollToday(_character.PendingInheritance, scope));
                case "tipo_trabajo":
                    return _character.Job == null ? Value.Null : Value.FromText(JobKindKey(_character.Job.Kind));
                case "sueldo":
                    return Value.FromNumber((double)(_character.Job?.Salary ?? 0));
                case "hijos_menores":
                    return Value.FromNumber(((EventScope)scope).World.ChildrenOf(_character.Id).Count(c => c.IsAlive && c.AgeInYears(date) < 18));
                case "hijos_en_casa":
                    return Value.FromNumber(CustodyRules.ChildrenAtHome(((EventScope)scope).World, _character));
                case "hijos_con_otro":
                    return Value.FromNumber(CustodyRules.ChildrenWithOtherParent(((EventScope)scope).World, _character));
                case "hijos_a_cargo":
                    return Value.FromNumber(CustodyRules.ChildrenInCare(((EventScope)scope).World, _character));
                case "hijos_por_acordar":
                    return Value.FromNumber(CustodyRules.ChildrenToAgree(((EventScope)scope).World, _character));
                case "coprogenitor":
                    Character? coParent = CustodyRules.CoParent(((EventScope)scope).World, _character);
                    return coParent == null ? Value.Null : Value.FromObject(new CharacterObject(coParent, _character));
                case "gastos":
                    return Value.FromNumber((double)_character.LastExpenses.Where(l => l.Amount > 0).Sum(l => l.Amount));
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
                case "prevision":
                    return TextOrNull(Health(scope) is Content.HealthCareDefinition coverageRules ? Economy.HealthCareRules.Coverage(coverageRules, _character) : null);
                case "prevision_privada":
                    return Value.FromBool(Health(scope) is Content.HealthCareDefinition privateRules && Economy.HealthCareRules.PlanOf(privateRules, _character) != null);
                case "tramo_salud":
                    return TextOrNull(Health(scope) is Content.HealthCareDefinition tierRules
                        ? Economy.HealthCareRules.Tier(tierRules, _character, MinimumWage(scope), PriceIndex(scope))
                        : null);
                case "precio_plan_salud":
                    return Value.FromNumber((double)PlanPriceToday(scope));
                case "semanas_desde_atencion":
                    return Value.FromNumber(_character.LastHealthCareDate.HasValue
                        ? ((EventScope)scope).World.Date.WeeksSince(_character.LastHealthCareDate.Value)
                        : NeverAttendedWeeks);
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
                case "en_tu_equipo":
                    return Value.FromBool(Work.WorkRules.Colleagues(((EventScope)scope).World, Viewer(member)).Any(c => c.Id == _character.Id));
                case "quiere_amistad":
                    return Value.FromBool(Education.SchoolRules.ReadyForFriendship(((EventScope)scope).World, Viewer(member), _character));
                case "recomendado":
                    return Value.FromBool(Work.WorkRules.InTeam(Viewer(member)) && Viewer(member).Team!.RecommendedIds.Contains(_character.Id));
                case "vive_contigo":
                    return Value.FromBool(Economy.HousingRules.Roommate(((EventScope)scope).World, Viewer(member))?.Id == _character.Id);
                case "tiene_negocio":
                    return Value.FromBool(_character.Venture != null);
                case "negocio_formal":
                    return Value.FromBool(_character.Venture?.Formal ?? false);
                case "meses_negocio":
                    return Value.FromNumber(_character.Venture?.MonthsOpen ?? 0);
                case "ganancia_negocio":
                    return Value.FromNumber((double)(_character.Venture?.LastShare ?? 0));
                case "ganado_negocio":
                    return Value.FromNumber((double)(_character.Venture?.TotalShare ?? 0));
                case "es_tu_socio":
                    return Value.FromBool(Viewer(member).Venture?.PartnerId == _character.Id);
                case "tiene_equipo":
                    return Value.FromBool(Work.WorkRules.InTeam(_character));
                case "comparte_arriendo":
                    return Value.FromBool(Economy.HousingRules.Roommate(((EventScope)scope).World, _character) != null);
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
                case "ultima_area":
                    return TextOrNull((_character.Job?.Position ?? _character.LastPosition)?.AreaId);
                case "meses_en_cargo":
                    return Value.FromNumber(_character.Job?.Position == null ? 0 : _character.Job.MonthsInPosition);
                case "desempeno":
                    return Value.FromNumber(_character.Job == null ? 0 : _character.Job.Performance);
                case "oficio":
                    return TextOrNull(Work.OccupationRules.CareerOf(((EventScope)scope).Content, _character.Job)?.Id);
                case "meta":
                    return Value.FromNumber(_character.Job?.GoalPercent ?? 0);
                case "en_desafio":
                    return Value.FromBool(_character.Job?.Challenge != null);
                case "meses_bajo_meta":
                    return Value.FromNumber(_character.Job?.MonthsUnderGoal ?? 0);
                case "amonestaciones":
                    return Value.FromNumber(_character.Job?.Warnings ?? 0);
                case "dias_vacaciones":
                    return Value.FromNumber(Math.Floor(_character.Job?.VacationDays ?? 0));
                case "renuncio":
                    return Value.FromBool(_character.Job?.LeavingWeek != null);
                case "semanas_para_irse":
                    return Value.FromNumber(_character.Job?.LeavingWeek == null ? 0 : Math.Max(0, _character.Job.LeavingWeek.Value - ((EventScope)scope).World.Date.TotalWeeks));
                case "pago_vacaciones":
                    return Value.FromNumber((double)VacationPay((EventScope)scope));
                case "pago_variable":
                    return Value.FromNumber((double)((_character.Job?.VariablePay ?? 0) + (_character.Job?.Tips ?? 0)));
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
                case "mensajes_sin_leer":
                    return Value.FromNumber(_character.Chats?.UnreadCount ?? 0);
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

        /// <summary>El sistema de salud del país donde vive, o null.</summary>
        private Content.HealthCareDefinition? Health(IScriptScope scope) =>
            Economy.HealthCareRules.RulesFor(((EventScope)scope).Content, _character);

        private double PriceIndex(IScriptScope scope) =>
            _character.Residence == null ? 1 : ((EventScope)scope).World.EconomyOf(_character.Residence.CountryId)?.PriceIndex ?? 1;

        private decimal MinimumWage(IScriptScope scope) =>
            _character.Residence == null ? 0 : ((EventScope)scope).Content.GetCountry(_character.Residence.CountryId).Economy?.Salaries?.Minimum ?? 0;

        /// <summary>Lo que cuesta al mes su plan privado a su edad, a precios de hoy (0 si está en la pública).</summary>
        private decimal PlanPriceToday(IScriptScope scope)
        {
            Content.HealthCareDefinition? rules = Health(scope);
            Content.HealthPlanDefinition? plan = rules == null ? null : Economy.HealthCareRules.PlanOf(rules, _character);
            return plan == null
                ? 0
                : Economy.PayrollCalculator.ToToday(Economy.HealthCareRules.PlanPrice(rules!, plan, _character.AgeInYears(((EventScope)scope).World.Date)), PriceIndex(scope));
        }

        private static Value TextOrNull(string? text) => text == null ? Value.Null : Value.FromText(text);

        /// <summary>
        /// jugador.unirse('union.chile.matrimonio'): se casa (o firma la unión civil) con su
        /// pareja actual, en ambos sentidos. La unión debe existir en su país y no puede
        /// estar unido con otra persona (primero se divorcia).
        /// </summary>
        private void Unite(EventScope scope, string unionId)
        {
            scope.RequireWritable("jugador." + UniteMethod);
            if (_character.Residence == null || !scope.Content.GetCountry(_character.Residence.CountryId).CivilUnions.Contains(unionId))
            {
                throw new ScriptException($"jugador.unirse('{unionId}'): esa unión no existe en el país donde vive (uniones_civiles).");
            }

            if (!_character.PartnerId.HasValue || !scope.World.TryGetCharacter(_character.PartnerId.Value, out Character? partner))
            {
                throw new ScriptException("jugador.unirse(): no tiene pareja; revisa jugador.pareja != nada antes.");
            }

            if (LivingSpouse(scope) != null && _character.SpouseId != partner.Id)
            {
                throw new ScriptException("jugador.unirse(): ya está unido con otra persona; revisa jugador.union == nada antes.");
            }

            _character.SpouseId = partner.Id;
            _character.UnionTypeId = unionId;
            partner.SpouseId = _character.Id;
            partner.UnionTypeId = unionId;
        }

        /// <summary>jugador.terminar_union(): termina la unión ante la ley (la sentencia de divorcio, el aviso de término), para los dos.</summary>
        private void EndUnion(EventScope scope)
        {
            if (_character.SpouseId.HasValue && scope.World.TryGetCharacter(_character.SpouseId.Value, out Character? spouse) && spouse.SpouseId == _character.Id)
            {
                spouse.SpouseId = null;
                spouse.UnionTypeId = null;
            }

            _character.SpouseId = null;
            _character.UnionTypeId = null;
        }

        /// <summary>Con quién está unido ante la ley, si sigue vivo (si murió, ya no hay unión: quedó viudo).</summary>
        private Character? LivingSpouse(IScriptScope scope) =>
            _character.SpouseId.HasValue && _character.UnionTypeId != null
            && ((EventScope)scope).World.TryGetCharacter(_character.SpouseId.Value, out Character? spouse) && spouse.IsAlive
                ? spouse
                : null;

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
        /// <summary>Lo que le pagarían por las vacaciones no tomadas (de su trabajo, o del último).</summary>
        private decimal VacationPay(EventScope scope)
        {
            Economy.Job? job = _character.Job ?? _character.LastJob;
            if (job == null || _character.Residence == null)
            {
                return 0;
            }

            return Work.VacationRules.PayFor(scope.Content.GetCountry(_character.Residence.CountryId).Economy?.Vacations, job);
        }

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
                    if (_character.Job == null)
                    {
                        throw new ScriptException("jugador.desempeno solo se puede cambiar si trabaja (revisa jugador.trabaja).");
                    }

                    _character.Job.Performance = number;
                    break;
                case "amonestaciones":
                    if (_character.Job == null)
                    {
                        throw new ScriptException("jugador.amonestaciones solo se puede cambiar si trabaja (revisa jugador.trabaja).");
                    }

                    _character.Job.Warnings = Math.Max(0, number);
                    break;
                case "meses_bajo_meta":
                    if (_character.Job == null)
                    {
                        throw new ScriptException("jugador.meses_bajo_meta solo se puede cambiar si trabaja (revisa jugador.trabaja).");
                    }

                    _character.Job.MonthsUnderGoal = Math.Max(0, number);
                    break;
                case "felicidad":
                    _character.Stats.Happiness = number;
                    break;
                case "inteligencia":
                    _character.Stats.Smarts = number;
                    break;
                case "energia":
                    _character.Stats.Energy = number;
                    break;
                case "apariencia":
                    _character.Stats.Looks = number;
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

        /// <summary>La escala de notas del país donde vive, o null.</summary>
        private Content.GradeScaleDefinition? Scale(IScriptScope scope) =>
            _character.Residence == null ? null : ((EventScope)scope).Content.GetCountry(_character.Residence.CountryId).GradeScale;

        public Value Call(string method, Value[] args, IScriptScope scope)
        {
            switch (method)
            {
                case "preparar_prueba":
                    {
                        ((EventScope)scope).RequireWritable("jugador.preparar_prueba");
                        Education.SchoolRecord? record = _character.SchoolRecord;
                        if (record == null || !record.TestPending)
                        {
                            throw new ScriptException("jugador.preparar_prueba solo sirve con una prueba anunciada (revisa jugador.prueba_anunciada).");
                        }

                        if (args.Length != 1 || args[0].Kind != ValueKind.Number)
                        {
                            throw new ScriptException("jugador.preparar_prueba necesita un número de 0 a 3.");
                        }

                        record.TestPreparation = Math.Max(record.TestPreparation, Math.Max(0, Math.Min(Education.GradeRules.MaxPreparation, (int)args[0].AsNumber("preparar_prueba"))));
                        return Value.Null;
                    }
                case "estudiar_semestre":
                    {
                        ((EventScope)scope).RequireWritable("jugador.estudiar_semestre");
                        RequireStudies(method);
                        if (args.Length != 1 || args[0].Kind != ValueKind.Number)
                        {
                            throw new ScriptException("jugador.estudiar_semestre necesita un número (por ejemplo 1 si estudia, -1 si no).");
                        }

                        CareerStudies studies = _character.Studies!;
                        int max = Education.UniversityRules.MaxPreparation;
                        studies.Preparation = Math.Max(-max, Math.Min(max, studies.Preparation + (int)args[0].AsNumber("estudiar_semestre")));
                        return Value.Null;
                    }
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
                case UniteMethod:
                    Unite((EventScope)scope, EventScope.SingleText("jugador." + method, args));
                    return Value.Null;
                case "recibir_herencia":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.recibir_herencia");
                    _character.Money += PayrollToday(_character.PendingInheritance, scope);
                    _character.PendingInheritance = 0;
                    ((EventScope)scope).World.WriteJournal(_character.Id, Economy.InheritanceSettlement.ReceivedJournal);
                    return Value.Null;
                case "quedarse_con_hijos":
                case "hijos_con_el_otro":
                case "cuidado_compartido":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    if (method == "quedarse_con_hijos")
                    {
                        CustodyRules.KeepChildren(((EventScope)scope).World, _character);
                    }
                    else if (method == "hijos_con_el_otro")
                    {
                        CustodyRules.GiveChildren(((EventScope)scope).World, _character);
                    }
                    else
                    {
                        CustodyRules.ShareCustody(((EventScope)scope).World, _character);
                    }

                    return Value.Null;
                case "terminar_union":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.terminar_union");
                    EndUnion((EventScope)scope);
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
                case ContractMethod:
                    string option = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Economy.ServiceRules.Contract(((EventScope)scope).Content, _character, option);
                    return Value.Null;
                case ServiceMethod:
                    string serviceId = EventScope.SingleText("jugador." + method, args);
                    return _character.Services.TryGetValue(serviceId, out string? contracted) ? Value.FromText(contracted) : Value.Null;
                case CancelServiceMethod:
                    string cancelled = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Economy.ServiceRules.Cancel(_character, cancelled);
                    return Value.Null;
                case JoinHealthMethod:
                    string coverage = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Economy.HealthCareRules.Join(((EventScope)scope).Content, _character, coverage);
                    return Value.Null;
                case CanAttendMethod:
                    string usable = EventScope.SingleText("jugador." + method, args);
                    Content.HealthCareDefinition? usableRules = Health(scope);
                    Content.HealthCareServiceDefinition? usableService = usableRules?.Service(usable);
                    return Value.FromBool(usableRules != null && usableService != null && Economy.HealthCareRules.CanUse(usableRules, _character, usableService));
                case AttendMethod:
                    string attention = EventScope.SingleText("jugador." + method, args);
                    ((EventScope)scope).RequireWritable("jugador." + method);
                    Economy.HealthCareRules.Attend(((EventScope)scope).World, ((EventScope)scope).Content, _character, attention);
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
                case "nuevo_contrato":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.nuevo_contrato");
                    if (_character.Job == null)
                    {
                        throw new ScriptException("jugador.nuevo_contrato(): no trabaja (revisa jugador.trabaja).");
                    }

                    // Un contrato nuevo con el mismo empleador (la obra siguiente de la constructora):
                    // la antigüedad vuelve a cero, pero el equipo es el mismo.
                    _character.Job.MonthsInJob = 0;
                    if (_character.Team != null)
                    {
                        _character.Team.MonthsInJob = 0;
                    }

                    return Value.Null;
                case "pedir_aumento":
                    ((EventScope)scope).RequireWritable("jugador.pedir_aumento");
                    if (args.Length != 1 || _character.Job == null)
                    {
                        throw new ScriptException("jugador.pedir_aumento(porcentaje) necesita que trabaje y cuánto pide: pedir_aumento(10).");
                    }

                    Work.RaiseRules.Ask(((EventScope)scope).World, ((EventScope)scope).Content, _character, ((EventScope)scope).Person, args[0].AsNumber("jugador.pedir_aumento()"));
                    return Value.Null;
                case "abrir_negocio":
                    ((EventScope)scope).RequireWritable("jugador.abrir_negocio");
                    if (args.Length != 2 || ((EventScope)scope).Person == null || _character.Venture != null)
                    {
                        throw new ScriptException("jugador.abrir_negocio(aporte, 'formal' o 'informal') necesita un evento con persona (con) y que no tenga otro negocio (revisa jugador.tiene_negocio).");
                    }

                    string form = args[1].AsText("jugador.abrir_negocio()");
                    if (form != "formal" && form != "informal")
                    {
                        throw new ScriptException($"jugador.abrir_negocio(): la forma '{form}' no existe (formal o informal).");
                    }

                    Content.VenturesDefinition ventureRules = (_character.Residence == null ? null : ((EventScope)scope).Content.GetCountry(_character.Residence.CountryId).Economy?.Ventures)
                        ?? throw new ScriptException("jugador.abrir_negocio(): en su país no hay economia.negocios.");
                    Work.VentureRules.Open(((EventScope)scope).World, ventureRules, _character, ((EventScope)scope).Person!, form == "formal",
                        Math.Round((decimal)args[0].AsNumber("jugador.abrir_negocio()"), MidpointRounding.AwayFromZero), PriceIndex((EventScope)scope));
                    return Value.Null;
                case "cerrar_negocio":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.cerrar_negocio");
                    Work.VentureRules.Close(((EventScope)scope).World, _character, failed: false);
                    return Value.Null;
                case "recomendar":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.recomendar");
                    if (!Work.WorkRules.InTeam(_character) || ((EventScope)scope).Person == null)
                    {
                        throw new ScriptException("jugador.recomendar() necesita un equipo en la pega y un evento con persona (con).");
                    }

                    Work.ReferralRules.Recommend(((EventScope)scope).World, ((EventScope)scope).Content, _character, ((EventScope)scope).Person!);
                    return Value.Null;
                case "compartir_arriendo":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.compartir_arriendo");
                    if (_character.Lease == null || ((EventScope)scope).Person == null)
                    {
                        throw new ScriptException("jugador.compartir_arriendo() necesita un arriendo (revisa jugador.vivienda) y un evento con persona (con).");
                    }

                    _character.Lease.RoommateId = ((EventScope)scope).Person!.Id;
                    return Value.Null;
                case "dejar_de_compartir":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.dejar_de_compartir");
                    if (_character.Lease != null)
                    {
                        _character.Lease.RoommateId = null;
                    }

                    return Value.Null;
                case "avisar_renuncia":
                    ((EventScope)scope).RequireWritable("jugador.avisar_renuncia");
                    if (args.Length != 1 || _character.Job == null)
                    {
                        throw new ScriptException("jugador.avisar_renuncia(semanas) necesita que trabaje y en cuántas semanas se va.");
                    }

                    _character.Job.LeavingWeek = ((EventScope)scope).World.Date.TotalWeeks + (int)Math.Round(args[0].AsNumber("jugador.avisar_renuncia()"), MidpointRounding.AwayFromZero);
                    return Value.Null;
                case "tomar_vacaciones":
                    ((EventScope)scope).RequireWritable("jugador.tomar_vacaciones");
                    double days = args.Length == 1 ? args[0].AsNumber("jugador.tomar_vacaciones()") : -1;
                    if (_character.Job == null || days <= 0 || days > _character.Job.VacationDays + 1e-9)
                    {
                        throw new ScriptException("jugador.tomar_vacaciones(días) necesita esos días ganados (revisa jugador.dias_vacaciones).");
                    }

                    _character.Job.VacationDays -= days;
                    return Value.Null;
                case "cobrar_vacaciones":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.cobrar_vacaciones");
                    Economy.Job? paidJob = _character.Job ?? _character.LastJob;
                    _character.Money += VacationPay((EventScope)scope);
                    if (paidJob != null)
                    {
                        paidJob.VacationDays = 0;
                    }

                    return Value.Null;
                case "desafio":
                    ((EventScope)scope).RequireWritable("jugador.desafio");
                    if (Work.OccupationRules.CareerOf(((EventScope)scope).Content, _character.Job) == null)
                    {
                        throw new ScriptException("jugador.desafio(): solo hay desafíos en un oficio (revisa jugador.oficio != nada).");
                    }

                    _character.Job!.Challenge = EventScope.SingleText("jugador.desafio", args);
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
                case "hijos_en_casa_entre":
                    return Value.FromNumber(ChildrenAtHomeBetween((EventScope)scope, args));
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
        /// hijos_en_casa_entre(desde, hasta): cuántos de sus hijos menores que viven en su casa
        /// tienen entre esas edades (años cumplidos, las dos incluidas). Sirve para cobrar el
        /// jardín o el colegio que el jugador eligió solo por los hijos que tienen esa edad.
        /// </summary>
        private int ChildrenAtHomeBetween(EventScope scope, Value[] args)
        {
            if (args.Length != 2)
            {
                throw new ScriptException("jugador.hijos_en_casa_entre() necesita dos edades: hijos_en_casa_entre(4, 17).");
            }

            double from = args[0].AsNumber("jugador.hijos_en_casa_entre()");
            double to = args[1].AsNumber("jugador.hijos_en_casa_entre()");
            return CustodyRules.ChildrenAtHomeAged(scope.World, _character, scope.World.Date, (int)from, (int)to);
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
                Economy.Company? company = Economy.CompanyRules.Hire(scope.World, scope.Content, _character, career, nameId: search.CompanyNameId);
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
            Content.CareerDefinition career = scope.Content.GetCareer(area.CareerId);
            decimal salary = Economy.CareerLadder.Salary(scope.SalaryBasisOf(_character, career), area, position);
            if (size != null)
            {
                // En un oficio el sueldo base ya es el piso (el mínimo o el convenio): una empresa
                // chica no paga menos, una grande puede pagar algo más.
                double relative = career.IsOccupation ? Math.Max(1, size.SalaryRelative) : size.SalaryRelative;
                salary = Math.Round(salary * (decimal)relative, MidpointRounding.AwayFromZero);
            }

            salary = Math.Round(salary * raise, MidpointRounding.AwayFromZero);

            Economy.Job? previous = _character.Job;
            Economy.Job job = Economy.CareerLadder.Place(_character, kind, salary, PriceIndex(scope), area, position);
            job.CompanyId = company?.Id;
            if (previous?.CompanyId != null && previous.CompanyId == job.CompanyId)
            {
                // Un ascenso o un cambio de cargo en la misma empresa no borra la antigüedad.
                job.MonthsInJob = previous.MonthsInJob;
            }
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

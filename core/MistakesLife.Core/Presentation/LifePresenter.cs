using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Puente entre una partida y cualquier pantalla (Unity, pruebas).
    /// La pantalla pide una <see cref="LifeSnapshot"/> para dibujar y llama a los comandos
    /// (avanzar, elegir, seguir con un hijo). Toda la lógica queda en el núcleo.
    /// Los textos salen de las tablas de idioma (claves ui.*), nunca del código.
    /// </summary>
    public sealed class LifePresenter
    {
        /// <summary>
        /// Al avanzar, el tiempo corre hasta que pasa algo, pero nunca más de un año
        /// seguido, para que el jugador vea pasar su vida (cada cumpleaños, cada cambio en la casa).
        /// Para ir más lento está <see cref="AdvanceWeek"/>.
        /// </summary>
        public const int MaxYearsPerStep = 1;

        /// <summary>Claves de texto que usa la pantalla; deben existir en el idioma base.</summary>
        public static readonly IReadOnlyList<string> TextKeys = new[]
        {
            "ui.edad.semanas", "ui.edad.un_ano", "ui.edad.anos", "ui.fecha.hoy", "ui.fecha.dia", "ui.fecha.otro_ano",
            "ui.horario.titulo", "ui.horario.libre", "ui.horario.tarde", "ui.horario.noche", "ui.horario.sube", "ui.horario.baja", "ui.horario.igual", "ui.horario.nuevo", "ui.horario.nuevo_ano",
            "ui.horario.dia.0", "ui.horario.dia.1", "ui.horario.dia.2", "ui.horario.dia.3", "ui.horario.dia.4", "ui.horario.dia.5", "ui.horario.dia.6",
            "ui.momento.con", "ui.momento.madre", "ui.momento.padre", "ui.regla.siempre",
            "ui.momento.profe.f", "ui.momento.profe.m", "ui.momento.jefe.f", "ui.momento.jefe.m", "ui.curso.titulo", "ui.curso.profe_jefe",
            "ui.notas.titulo", "ui.notas.promedio", "ui.notas.sin_notas", "ui.notas.fin_ano",
            "ui.deseo.titulo", "ui.deseo.cumplido", "ui.deseo.perdido", "ui.deseo.paso",
            "ui.mes.1", "ui.mes.2", "ui.mes.3", "ui.mes.4", "ui.mes.5", "ui.mes.6", "ui.mes.7", "ui.mes.8", "ui.mes.9", "ui.mes.10", "ui.mes.11", "ui.mes.12",
            "ui.dinero", "ui.separador_miles",
            "ui.relacion.madre", "ui.relacion.padre", "ui.relacion.pareja", "ui.relacion.hija", "ui.relacion.hijo",
            "ui.boton.seguir_viviendo", "ui.boton.seguir_con", "ui.boton.nueva_vida", "ui.salud", "ui.felicidad", "ui.inteligencia",
            "ui.dinero_titulo", "ui.diario", "ui.familia", "ui.generacion", "ui.partida", "ui.legado", "ui.fin", "ui.fallecido",
            "ui.vista.casa", "ui.vista.barrio", "ui.vista.trabajo", "ui.trabajo.cargo", "ui.oficio.meta", "ui.pega.te_vas", "ui.pega.vacaciones.habiles", "ui.pega.vacaciones.corridos", "ui.pega.decides_tu", "ui.dinero.decides_tu", "ui.busqueda.una_empresa", "ui.lugar.casa", "ui.lugar.colegio", "ui.lugar.estudios", "ui.lugar.trabajo",
            "ui.boton.volver_pasado", "ui.boton.cancelar", "ui.pasado.titulo", "ui.pasado.cargas", "ui.pasado.etapa", "ui.pasado.decision", "ui.intuicion",
            "ui.gente", "ui.relacion.amistad", "ui.relacion.colega", "ui.relacion.rival", "ui.relacion.ex",
            "ui.sentir.muy_unidos", "ui.sentir.buena", "ui.sentir.tibia", "ui.sentir.distanciados", "ui.sentir.rencor",
            "ui.trabajo.empresa", "ui.trabajo.empresa_sin_nombre",
            "ui.paso.una_semana", "ui.paso.semanas", "ui.paso.un_ano", "ui.paso.anos", "ui.paso.un_ano_semanas", "ui.paso.anos_semanas",
            "ui.paso.titulo", "ui.paso.sin_cambios", "ui.momento_clave",
            "ui.nacer.nina", "ui.nacer.nino", "ui.hogar.justo", "ui.hogar.medio", "ui.hogar.acomodado",
            "ui.inicio.titulo", "ui.inicio.lema", "ui.inicio.continuar", "ui.inicio.nueva_vida", "ui.inicio.como_jugar",
            "ui.inicio.reemplazar", "ui.inicio.reemplazar_si", "ui.nacer.titulo", "ui.nacer.ayuda", "ui.nacer.elegir", "ui.nacer.otras",
            "ui.boton.menu", "ui.boton.una_semana", "ui.boton.siguiente", "ui.boton.entendido",
            "ui.tutorial.1", "ui.tutorial.2", "ui.tutorial.3", "ui.tutorial.4", "ui.tutorial.5",
            "ui.dinero.titulo", "ui.dinero.tienes", "ui.dinero.movimientos", "ui.dinero.sin_movimientos", "ui.dinero.sueldo", "ui.dinero.cuotas",
            "ui.dinero.salio", "ui.dinero.mes_en_dias", "ui.dinero.dias", "ui.dinero.un_dia", "ui.dinero.menos_de_un_dia", "ui.dinero.debes", "ui.dinero.atrasada",
            "ui.cosas.tu_gente", "ui.cosas.volver", "ui.vista.girar",
            "ui.origen.titulo", "ui.origen.elegiste", "ui.origen.volver",
            "ui.capitulo.nacer", "ui.capitulo.cumpli", "ui.capitulo.titulo", "ui.capitulo.abierto",
        };

        /// <summary>
        /// Cuánto tiene que cambiar el dinero (en pesos del juego) en un paso para mostrarlo
        /// en el resumen: los cambios de centavos no se cuentan.
        /// </summary>
        public const decimal MinVisibleMoneyChange = 1m;

        /// <summary>Desde qué nivel del hogar (0 a 100) se considera de clase media, y desde cuál acomodado.</summary>
        public const int MiddleHomeLevel = 35;

        public const int WealthyHomeLevel = 70;

        /// <summary>Cuántas personas fuera de la familia se muestran como máximo, para que la lista se pueda leer.</summary>
        public const int MaxPeople = 8;

        public LifePresenter(LifeSession session, string language = ContentDatabase.BaseLanguage)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Language = language;
        }

        public LifeSession Session { get; }

        /// <summary>Idioma de los textos (por ejemplo "es").</summary>
        public string Language { get; set; }

        /// <summary>En qué momento está la partida.</summary>
        public LifePhase Phase
        {
            get
            {
                if (!Session.World.Player.IsAlive)
                {
                    return Session.IsOver ? LifePhase.Over : LifePhase.ChoosingHeir;
                }

                return Session.Pending != null ? LifePhase.Choosing : LifePhase.Living;
            }
        }

        /// <summary>Foto actual de la partida, lista para dibujar.</summary>
        public LifeSnapshot Snapshot()
        {
            World world = Session.World;
            Character player = world.Player;
            LifePhase phase = Phase;
            return new LifeSnapshot(
                phase,
                FullName(player),
                Session.PlayerAgeInYears,
                AgeLabel(player, world.Date),
                Today(world.Date),
                Place(player),
                player.Stats.Health,
                player.Stats.Happiness,
                player.Stats.Smarts,
                player.Money,
                MoneyLabel(player.Money),
                world.Generation,
                world.Seed,
                phase == LifePhase.Choosing ? EventOf(Session.Pending!) : null,
                Family(player),
                Session.Heirs.Select(h => Person(h, ChildRelation(h))).ToList(),
                Home(player),
                PlaceOf(player),
                PlaceLabel(player),
                Workplace(player),
                People(player));
        }

        /// <summary>
        /// Adónde va el jugador en una semana normal: el trabajo manda sobre los estudios,
        /// y los estudios de una carrera sobre el colegio.
        /// </summary>
        public static DailyPlace PlaceOf(Character player)
        {
            if (player.Job != null)
            {
                return DailyPlace.Work;
            }

            if (player.Studies != null)
            {
                return DailyPlace.Studies;
            }

            return player.EducationStageId != null ? DailyPlace.School : DailyPlace.Home;
        }

        /// <summary>
        /// La frase de adónde va esta semana. En el colegio usa el nombre de la etapa del
        /// país ("Sala cuna", "Educación básica"), así una guagua no "va al colegio".
        /// </summary>
        private string PlaceLabel(Character player)
        {
            switch (PlaceOf(player))
            {
                case DailyPlace.Work:
                    return T("ui.lugar.trabajo");
                case DailyPlace.Studies:
                    return T("ui.lugar.estudios");
                case DailyPlace.School:
                    return Text("ui.lugar.colegio", T(player.EducationStageId!));
                default:
                    return T("ui.lugar.casa");
            }
        }

        /// <summary>Etapa de la vida para una edad en años.</summary>
        public static LifeStage StageOf(int ageInYears)
        {
            if (ageInYears < 2)
            {
                return LifeStage.Baby;
            }

            if (ageInYears < 12)
            {
                return LifeStage.Child;
            }

            if (ageInYears < 18)
            {
                return LifeStage.Teen;
            }

            return ageInYears < 65 ? LifeStage.Adult : LifeStage.Elder;
        }

        /// <summary>
        /// Avanza hasta que pase algo (un evento, la muerte), con un máximo de
        /// <see cref="MaxYearsPerStep"/> años. Devuelve las semanas que pasaron.
        /// </summary>
        public int AdvanceUntilSomething() => Advance(MaxYearsPerStep * GameDate.WeeksPerYear);

        /// <summary>
        /// Responde el evento pendiente y deja una regla propia ("hacer esto siempre"): si
        /// vuelve a pasar, se elige lo mismo sin detener el tiempo. Solo si <see cref="EventView.AllowsRule"/>.
        /// </summary>
        public void ChooseAlways(int optionPosition)
        {
            PendingEvent pending = Session.Pending ?? throw new InvalidOperationException("No hay un evento pendiente.");
            if (optionPosition < 0 || optionPosition >= pending.AvailableOptions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(optionPosition), $"El evento tiene {pending.AvailableOptions.Count} opciones.");
            }

            Session.ChooseAlways(pending.AvailableOptions[optionPosition]);
        }

        /// <summary>Las reglas propias del jugador, en el orden en que las hizo.</summary>
        public IReadOnlyList<RuleView> Rules()
        {
            var views = new List<RuleView>();
            foreach (OwnRule rule in Session.World.Player.OwnRules)
            {
                EventDefinition? sample = Session.Content.Events.FirstOrDefault(e => e.RuleKey == rule.Key);
                EventOptionDefinition? option = sample == null ? null : Session.Content.Events
                    .Where(e => e.RuleKey == rule.Key)
                    .SelectMany(e => e.Options)
                    .FirstOrDefault(o => o.RuleKey == rule.Choice);
                if (sample == null || option == null)
                {
                    continue;
                }

                string situation = sample.RuleGroup != null ? T(sample.RuleGroup) : T(sample.ResolvedTextKey);
                views.Add(new RuleView(rule.Key, situation, T(option.TextKey), rule.TimesUsed));
            }

            return views;
        }

        /// <summary>Borra una regla propia: la próxima vez se vuelve a preguntar.</summary>
        public void RemoveRule(string key) => Session.RemoveRule(key);

        /// <summary>
        /// Lo que quiere en esta etapa (la nota "lo que quiero"): lo abierto y lo que ya se
        /// cumplió o se perdió en la misma etapa, en orden.
        /// </summary>
        public IReadOnlyList<WishView> Wishes()
        {
            Character player = Session.World.Player;
            string? stage = player.Routine?.StageKey;
            var views = new List<WishView>();
            for (int i = 0; i < player.Wishes.Count; i++)
            {
                Wish wish = player.Wishes[i];
                WishDefinition? definition = Session.Content.Wish(wish.WishId);
                if (definition == null || wish.StageKey != stage)
                {
                    continue;
                }

                string? state = wish.State switch
                {
                    WishState.Done => T("ui.deseo.cumplido"),
                    WishState.Lost => T("ui.deseo.perdido"),
                    WishState.Faded => T("ui.deseo.paso"),
                    _ => null,
                };
                views.Add(new WishView(i, WithPerson(T(definition.Id), WishSystem.PersonOf(Session.World, wish)), wish.Highlighted, wish.State == WishState.Open, state));
            }

            return views;
        }

        /// <summary>Tu libreta de notas; null si nunca has tenido notas.</summary>
        public ReportCardView? ReportCard()
        {
            Character player = Session.World.Player;
            Education.SchoolRecord? record = player.SchoolRecord;
            GradeScaleDefinition? scale = player.Residence == null ? null : Session.Content.GetCountry(player.Residence.CountryId).GradeScale;
            if (player.Studies != null && scale != null)
            {
                return SemesterReportCard(player.Studies, scale);
            }

            if (record == null || scale == null)
            {
                return null;
            }

            bool finals = record.Marks.Count == 0;
            SortedDictionary<string, double> averages = finals ? record.LastFinals : Education.GradeRules.Finals(record, scale);
            var order = Session.Content.GetCountry(player.Residence!.CountryId).EducationStages
                .SelectMany(st => st.Subjects).Select(sub => sub.Id).Distinct().ToList();
            var subjects = averages
                .OrderBy(a => order.IndexOf(a.Key))
                .Select(a => new SubjectGradesView(
                    T(a.Key),
                    finals || !record.Marks.TryGetValue(a.Key, out List<double>? marks) ? new List<string>() : marks.Select(MarkText).ToList(),
                    MarkText(a.Value),
                    a.Value < scale.Pass))
                .ToList();
            double? general = Education.GradeRules.General(averages, scale);
            return new ReportCardView(subjects, general.HasValue ? MarkText(general.Value) : null, finals, record.RepeatedYears);
        }

        /// <summary>
        /// La libreta de la universidad, el CFT o el instituto: las notas finales del último
        /// semestre cerrado o, si todavía no cierra ninguno, los ramos del semestre que cursa.
        /// </summary>
        private ReportCardView SemesterReportCard(CareerStudies studies, GradeScaleDefinition scale)
        {
            if (studies.LastMarks.Count > 0)
            {
                CareerDefinition career = Session.Content.GetCareer(studies.CareerId);
                var order = career.Courses.Select(c => c.Id).ToList();
                var finals = studies.LastMarks
                    .OrderBy(m => order.IndexOf(m.Key))
                    .Select(m => new SubjectGradesView(CourseName(studies, m.Key, studies.LastMarksSemester), new List<string>(), MarkText(m.Value), m.Value < scale.Pass))
                    .ToList();
                string? last = studies.SemesterAverages.Count == 0 ? null : MarkText(studies.SemesterAverages[studies.SemesterAverages.Count - 1]);
                return new ReportCardView(finals, last, true, 0, semester: true);
            }

            int semester = studies.CompletedSemesters + 1;
            var courses = Education.UniversityRules.Courses(Session.Content, studies, semester)
                .Select(c => new SubjectGradesView(CourseName(studies, c.Id, semester), new List<string>(), string.Empty, false))
                .ToList();
            return new ReportCardView(courses, null, false, 0, semester: true);
        }

        /// <summary>El nombre de un ramo con su número, como en la malla: "Cálculo II".</summary>
        private string CourseName(CareerStudies studies, string courseId, int semester)
        {
            CourseDefinition? course = Session.Content.GetCareer(studies.CareerId).Courses.FirstOrDefault(c => c.Id == courseId);
            int number = course?.Number(semester) ?? 0;
            return number > 0 ? T(courseId) + " " + Education.UniversityRules.Roman(number) : T(courseId);
        }

        /// <summary>El último momento que el jugador vivió con cada persona (por id).</summary>
        private static IReadOnlyDictionary<int, Events.EventRecord> LastMomentWith(World world, Character player)
        {
            var last = new Dictionary<int, Events.EventRecord>();
            foreach (Events.EventRecord record in world.EventHistory)
            {
                if (record.CharacterId == player.Id && record.PersonId.HasValue && record.ChosenOption.HasValue)
                {
                    last[record.PersonId.Value] = record;
                }
            }

            return last;
        }

        /// <summary>
        /// Un momento vivido con alguien, en una línea: la línea del diario que dejó lo que elegiste
        /// o, si no dejó ninguna, "Elegiste: «...»". Nada inventado: sale de lo que pasó.
        /// </summary>
        private string? Memory(Events.EventRecord record, Character person)
        {
            if (!Session.Content.TryGetEvent(record.EventId, out EventDefinition? definition) || record.ChosenOption!.Value >= definition!.Options.Count)
            {
                return null;
            }

            EventOptionDefinition option = definition.Options[record.ChosenOption.Value];
            foreach (string effect in option.Effects)
            {
                System.Text.RegularExpressions.Match journal = System.Text.RegularExpressions.Regex.Match(effect, @"^\s*diario\(\s*'([^']+)'\s*\)\s*$");
                if (journal.Success)
                {
                    return WithPerson(T(journal.Groups[1].Value), person);
                }
            }

            return Text("ui.origen.elegiste", WithPerson(T(option.TextKey), person));
        }

        /// <summary>
        /// Tu malla: todos los semestres del plan que estudias, con los aprobados, el que cursas
        /// (o repites, con los ramos que quedaron rojos) y los que faltan. Null si no estudias una carrera.
        /// </summary>
        public StudyPlanView? StudyPlan()
        {
            Character player = Session.World.Player;
            CareerStudies? studies = player.Studies;
            if (studies == null)
            {
                return null;
            }

            GradeScaleDefinition? scale = player.Residence == null ? null : Session.Content.GetCountry(player.Residence.CountryId).GradeScale;
            int current = studies.CompletedSemesters + 1;
            bool retaking = studies.LastMarksSemester == current && scale != null && studies.LastMarks.Values.Any(m => m < scale.Pass);
            var semesters = new List<PlanSemesterView>();
            for (int semester = 1; semester <= studies.RequiredSemesters; semester++)
            {
                int number = semester;
                var courses = Education.UniversityRules.Courses(Session.Content, studies, number)
                    .Select(c => CourseName(studies, c.Id, number))
                    .ToList();
                PlanSemesterState state = semester < current ? PlanSemesterState.Approved
                    : semester > current ? PlanSemesterState.Future
                    : retaking ? PlanSemesterState.Retaking
                    : PlanSemesterState.Current;
                var failed = state == PlanSemesterState.Retaking
                    ? studies.LastMarks.Where(m => m.Value < scale!.Pass).Select(m => CourseName(studies, m.Key, number) + " · " + MarkText(m.Value)).ToList()
                    : new List<string>();
                semesters.Add(new PlanSemesterView(semester, Text("ui.malla.semestre", Education.UniversityRules.Roman(semester)), state, courses, failed));
            }

            string progress = Text("ui.malla.progreso", Math.Min(current, studies.RequiredSemesters), studies.RequiredSemesters, studies.SemestersTaken);
            return new StudyPlanView(T(studies.CareerId), progress, semesters);
        }

        /// <summary>Tu equipo del trabajo; null si no trabajas con equipo.</summary>
        public TeamView? Team()
        {
            World world = Session.World;
            Character player = world.Player;
            if (!Work.WorkRules.InTeam(player))
            {
                return null;
            }

            IReadOnlyDictionary<int, Events.EventRecord> lastMoments = LastMomentWith(world, player);
            ClassmateView Person(Character c, string? bond) => new ClassmateView(
                c.Id,
                c.FirstName + " " + c.LastName.Split(' ')[0],
                bond,
                RelationshipRules.Feelings(world, player, c).Affection,
                lastMoments.TryGetValue(c.Id, out Events.EventRecord? moment) ? Memory(moment, c) : null);
            string? BondWord(Character c) =>
                RelationshipRules.Feelings(world, player, c).Bond is RelationshipBond b && b != RelationshipBond.Colleague && RelationshipRules.BondKey(b) is string key
                    ? T("ui.relacion." + key)
                    : null;

            Character? boss = Work.WorkRules.Boss(world, player);
            var colleagues = Work.WorkRules.Colleagues(world, player)
                .OrderByDescending(c => RelationshipRules.Feelings(world, player, c).Affection)
                .ThenBy(c => c.Id)
                .Select(c => Person(c, BondWord(c)))
                .ToList();
            var left = player.Team!.LeftIds
                .Where(id => world.TryGetCharacter(id, out Character? _))
                .Select(id => Person(world.GetCharacter(id), T("ui.relacion.se_fue")))
                .ToList();
            string? bossKey = Work.OccupationRules.CareerOf(Session.Content, player.Job)?.Occupation!.BossKey;
            string bossTag = bossKey != null
                ? T(bossKey + (boss?.Gender == Gender.Female ? ".f" : ".m"))
                : T(boss?.Gender == Gender.Female ? "ui.pega.jefa" : "ui.pega.jefe_m");
            var actions = Session.Requestable()
                .Where(e => e.Place == WorkplacePlace)
                .Select(e => new ActionView(e.Id, T(e.OnRequest!.TextKey)))
                .ToList();
            return new TeamView(boss == null ? null : Person(boss, bossTag), colleagues, left, actions, ContractStatus(player));
        }

        /// <summary>
        /// Lo que puedes pedir ahora con esa persona (recomendarla en tu pega, proponerle vivir
        /// juntos...): los botones de su línea en "Tu gente". Los de la pega con tu jefe van en el equipo.
        /// </summary>
        public IReadOnlyList<ActionView> PersonActions(int personId) =>
            Session.RequestableWith(personId)
                .Where(e => e.Place != WorkplacePlace)
                .Select(e => new ActionView(e.Id, T(e.OnRequest!.TextKey)))
                .ToList();

        /// <summary>El lugar de los momentos de la pega: sus botones van en la pantalla del equipo.</summary>
        public const string WorkplacePlace = "lugar.trabajo";

        /// <summary>
        /// Lo que puedes decidir ahora sobre tu plata (revisar la compra, el plan, el transporte,
        /// repactar una deuda, irte de la casa...): los botones de "Tu dinero". Antes llegaban
        /// solos cada año; ahora los pides tú cuando quieres (regla de repeticiones, R7b).
        /// </summary>
        public IReadOnlyList<ActionView> MoneyActions() =>
            Session.Requestable()
                .Where(e => e.Place == WalletPlace)
                .Select(e => new ActionView(e.Id, T(e.OnRequest!.TextKey)))
                .ToList();

        /// <summary>El lugar de las decisiones de plata: sus botones van en Tu dinero.</summary>
        public const string WalletPlace = "lugar.dinero";

        /// <summary>"Te vas en 3 semanas" si renunciaste con aviso; si no, los días de vacaciones ganados.</summary>
        private string? ContractStatus(Character player)
        {
            Job? job = player.Job;
            if (job == null)
            {
                return null;
            }

            if (job.LeavingWeek.HasValue)
            {
                return Text("ui.pega.te_vas", Math.Max(0, job.LeavingWeek.Value - Session.World.Date.TotalWeeks));
            }

            VacationsDefinition? vacations = player.Residence == null ? null : Session.Content.GetCountry(player.Residence.CountryId).Economy?.Vacations;
            if (job.Kind != JobKind.Employee || vacations == null)
            {
                return null;
            }

            return Text("ui.pega.vacaciones." + vacations.Unit, (int)Math.Floor(job.VacationDays));
        }

        /// <summary>Tu curso del colegio; null si no vas a un curso.</summary>
        public SchoolClassView? SchoolClass()
        {
            World world = Session.World;
            Character player = world.Player;
            Education.SchoolClass? cls = player.SchoolClass;
            if (cls == null)
            {
                return null;
            }

            Character? teacher = Education.SchoolRules.Teacher(world, player);
            string? teacherName = teacher == null ? null
                : Text(teacher.Gender == Gender.Female ? "ui.momento.profe.f" : "ui.momento.profe.m", teacher.FirstName + " " + teacher.LastName.Split(' ')[0]);
            IReadOnlyDictionary<int, Events.EventRecord> lastMoments = LastMomentWith(world, player);
            var mates = Education.SchoolRules.Classmates(world, player)
                .Select(c => (Person: c, Feelings: RelationshipRules.Feelings(world, player, c)))
                .OrderByDescending(x => x.Feelings.Affection)
                .ThenBy(x => x.Person.Id)
                .Select(x => new ClassmateView(
                    x.Person.Id,
                    x.Person.FirstName + " " + x.Person.LastName.Split(' ')[0],
                    RelationshipRules.BondKey(x.Feelings.Bond) is string bond ? T("ui.relacion." + bond) : null,
                    x.Feelings.Affection,
                    lastMoments.TryGetValue(x.Person.Id, out Events.EventRecord? moment) ? Memory(moment, x.Person) : null))
                .ToList();
            var left = cls.LeftIds
                .Where(id => world.TryGetCharacter(id, out Character? _))
                .Select(id => world.GetCharacter(id))
                .Select(c => new ClassmateView(
                    c.Id,
                    c.FirstName + " " + c.LastName.Split(' ')[0],
                    T("ui.relacion.se_fue"),
                    RelationshipRules.Feelings(world, player, c).Affection,
                    lastMoments.TryGetValue(c.Id, out Events.EventRecord? moment) ? Memory(moment, c) : null))
                .ToList();
            bool section = player.Studies != null && Education.UniversityRules.InSection(player);
            string name = section
                ? Text("ui.seccion", cls.Level, cls.Letter, T(player.Studies!.CareerId))
                : Text("curso." + cls.StageId, cls.Level, cls.Letter);
            return new SchoolClassView(name, cls.Students, teacherName, mates, section, left);
        }

        /// <summary>Marca (o desmarca) con destacador algo que quiere (posición de <see cref="WishView.Index"/>).</summary>
        public void HighlightWish(int index, bool highlighted) => Session.HighlightWish(index, highlighted);

        /// <summary>Avanza una sola semana (o nada, si hay que decidir algo primero).</summary>
        public int AdvanceWeek() => Advance(1);

        /// <summary>
        /// "Seguir un mes": avanza hasta el comienzo del mes que viene, o antes si pasa algo
        /// importante (un evento, la muerte). Devuelve las semanas que pasaron.
        /// </summary>
        public int AdvanceMonth() => Advance(Session.World.Date.WeeksToNextMonth);

        /// <summary>
        /// "Un año": avanza un año, o menos si pasa algo importante. Es lo mismo que
        /// <see cref="AdvanceUntilSomething"/> (como mucho <see cref="MaxYearsPerStep"/> año).
        /// </summary>
        public int AdvanceYear() => AdvanceUntilSomething();

        /// <summary>
        /// Lo de arriba de la pantalla principal (docs/diseno-juego.md): el mes y el día, y en
        /// qué estás (tu curso, tu carrera o tu pega) y en qué comuna vives.
        /// </summary>
        public HeaderView Header()
        {
            World world = Session.World;
            Character player = world.Player;
            GameDate date = world.Date;
            string month = T("ui.mes." + date.Month);
            month = month.Length > 0 ? char.ToUpperInvariant(month[0]) + month.Substring(1) : month;
            string day = Text("ui.cabecera.dia", date.DayOfMonth);
            string commune = player.Residence == null ? string.Empty : T(player.Residence.CommuneId);
            return new HeaderView(month, day, RoleLabel(player), commune);
        }

        /// <summary>
        /// Las tardes libres del mes que le quedan para hacer cosas con el tiempo parado (D3),
        /// o null si todavía no tiene horario (antes de los 6 años).
        /// </summary>
        public FreeTimeView? FreeTime()
        {
            World world = Session.World;
            Character player = world.Player;
            if (player.Routine == null || !player.IsAlive)
            {
                return null;
            }

            int total = Life.FreeTimeRules.Total(Session.Content, player);
            int left = Life.FreeTimeRules.Left(world, Session.Content, player);
            string month = T("ui.mes." + world.Date.Month);
            string label = left == 1 ? Text("ui.tardes.una", month) : Text("ui.tardes.varias", left, month);
            string note = total == 0 ? T("ui.tardes.sin_horario_libre")
                : left == 0 ? T("ui.tardes.no_quedan")
                : Text("ui.tardes.deja", total);
            return new FreeTimeView(left, total, label, note);
        }

        /// <summary>
        /// Lo que puede hacer ahora con el tiempo parado en un grupo (gente, estudios, trabajo,
        /// salud, dinero; ver <see cref="Content.ActionDefinition.Groups"/>). Vacío si hay un evento
        /// esperando: primero se decide el momento.
        /// </summary>
        public IReadOnlyList<DoableActionView> Actions(string group)
        {
            World world = Session.World;
            Character player = world.Player;
            if (Session.Pending != null)
            {
                return new List<DoableActionView>();
            }

            return Life.ActionRules.Available(world, Session.Content, player, group)
                .Select(o => new DoableActionView(
                    o.Definition.Id,
                    o.Person?.Id,
                    WithPerson(T(o.Definition.Id), o.Person),
                    WithPerson(T(o.Definition.WhyKey), o.Person),
                    o.Definition.Effects.Select(p => Change(p.Key, p.Value, o.Person)).ToList(),
                    o.Definition.Afternoons == 1 ? T("ui.tardes.costo_una") : Text("ui.tardes.costo", o.Definition.Afternoons),
                    Life.ActionRules.CanAfford(world, Session.Content, player, o.Definition)))
                .ToList();
        }

        /// <summary>Hace la acción (con esa persona, si es con alguien) y cuenta lo que pasó, al tiro.</summary>
        public ActionResultView DoAction(string actionId, int? personId)
        {
            World world = Session.World;
            Content.ActionDefinition action = Session.Content.Action(actionId)
                ?? throw new InvalidOperationException($"No existe la acción '{actionId}'.");
            Character? person = personId.HasValue ? world.GetCharacter(personId.Value) : null;
            IReadOnlyList<Life.ActionChange> changes = Session.DoAction(actionId, personId);
            return new ActionResultView(
                Text("ui.fecha.dia", world.Date.DayOfMonth, T("ui.mes." + world.Date.Month)),
                WithPerson(T(action.Id), person),
                WithPerson(T(action.ResultKey), person),
                changes.Select(c => Change(c.Key, c.Delta, person)).ToList(),
                FreeTime()!);
        }

        /// <summary>Cómo se lee un cambio de una acción: "Inteligencia +2", "Energía −3", "La Javiera te quiere más".</summary>
        private ChangeView Change(string key, int delta, Character? person)
        {
            bool good = delta > 0;
            switch (key)
            {
                case "carino":
                    return new ChangeView(Text(good ? "ui.accion.carino_sube" : "ui.accion.carino_baja", person?.FirstName ?? string.Empty), good);
                case "preparar_prueba":
                    return new ChangeView(T("ui.accion.prueba_preparada"), good);
                default:
                    string amount = delta > 0 ? "+" + delta.ToString(CultureInfo.InvariantCulture) : "\u2212" + (-delta).ToString(CultureInfo.InvariantCulture);
                    return new ChangeView(T("ui.barra." + key) + " " + amount, good);
            }
        }

        /// <summary>En qué estás: el curso del colegio, la carrera que estudias o tu pega; vacío si nada.</summary>
        private string RoleLabel(Character player)
        {
            SchoolClassView? cls = SchoolClass();
            if (cls != null)
            {
                return cls.Name;
            }

            if (player.Studies != null)
            {
                return T(player.Studies.CareerId);
            }

            WorkplaceView? work = Workplace(player);
            return work?.Caption ?? string.Empty;
        }

        /// <summary>
        /// Las barras de la vida de la pantalla principal, en su orden, con su porcentaje y su
        /// nivel (verde, amarillo o rojo): Felicidad, Salud, Inteligencia, Apariencia y Energía.
        /// </summary>
        public IReadOnlyList<LifeBarView> LifeBars()
        {
            Stats stats = Session.World.Player.Stats;
            return new List<LifeBarView>
            {
                Bar("felicidad", stats.Happiness),
                Bar("salud", stats.Health),
                Bar("inteligencia", stats.Smarts),
                Bar("apariencia", stats.Looks),
                Bar("energia", stats.Energy),
            };
        }

        private LifeBarView Bar(string id, int value) => new LifeBarView(id, Text("ui.barra." + id), value, LifeBarView.LevelOf(value));

        /// <summary>Responde el evento pendiente con la opción en esa posición de <see cref="EventView.Options"/>.</summary>
        public void Choose(int optionPosition)
        {
            PendingEvent pending = Session.Pending ?? throw new InvalidOperationException("No hay un evento pendiente.");
            if (optionPosition < 0 || optionPosition >= pending.AvailableOptions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(optionPosition), $"El evento tiene {pending.AvailableOptions.Count} opciones.");
            }

            Session.Choose(pending.AvailableOptions[optionPosition]);
        }

        /// <summary>
        /// Volver al pasado: cargas que quedan y los momentos a los que se puede volver
        /// ahora (fotos del mundo permitidas por <see cref="TimeMachine.CanGoBackTo"/>).
        /// </summary>
        public TimeTravelView TimeTravel()
        {
            TimeMachine machine = Session.TimeMachine;
            var moments = machine.Snapshots
                .Where(machine.CanGoBackTo)
                .Select(s => new MomentView(s.Id, s.PlayerAgeInYears, MomentLabel(s)))
                .ToList();
            return new TimeTravelView(machine.Charges, Text("ui.pasado.cargas", machine.Charges), moments);
        }

        /// <summary>
        /// Vuelve al pasado, al momento en esa posición de <see cref="TimeTravelView.Moments"/>.
        /// Gasta una carga. Después conviene pedir el diario completo de nuevo
        /// (<see cref="FullJournal"/>): ese futuro ya no pasó.
        /// </summary>
        public void GoBack(int momentPosition)
        {
            IReadOnlyList<MomentView> moments = TimeTravel().Moments;
            if (momentPosition < 0 || momentPosition >= moments.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(momentPosition), $"Hay {moments.Count} momentos a los que volver.");
            }

            Session.GoBack(moments[momentPosition].SnapshotId);
        }

        /// <summary>Legado: sigue jugando con el hijo en esa posición de <see cref="LifeSnapshot.Heirs"/>.</summary>
        public void ContinueWith(int heirPosition)
        {
            IReadOnlyList<Character> heirs = Session.Heirs;
            if (heirPosition < 0 || heirPosition >= heirs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(heirPosition), $"Hay {heirs.Count} hijos para elegir.");
            }

            Session.ContinueWith(heirs[heirPosition].Id);
        }

        /// <summary>Líneas nuevas del diario desde la última vez que se pidieron.</summary>
        public IReadOnlyList<JournalLine> TakeNewJournal() =>
            Session.TakeNewJournalEntries().Select(Line).ToList();

        /// <summary>El diario completo del jugador actual.</summary>
        public IReadOnlyList<JournalLine> FullJournal() =>
            Session.World.Journal.Where(e => e.CharacterId == Session.World.PlayerId).Select(Line).ToList();

        /// <summary>
        /// El diario de noticias: lo último que salió en el país del jugador (y en su
        /// ciudad), de la más nueva a la más vieja, como mucho <paramref name="max"/>.
        /// </summary>
        public IReadOnlyList<NewsView> News(int max = 12)
        {
            World world = Session.World;
            Residence? residence = world.Player.Residence;
            var views = new List<NewsView>();
            if (residence == null)
            {
                return views;
            }

            for (int i = world.News.Count - 1; i >= 0 && views.Count < max; i--)
            {
                NewsRecord record = world.News[i];
                if (record.CountryId == residence.CountryId && (record.CityId == null || record.CityId == residence.CityId))
                {
                    views.Add(NewsOf(record));
                }
            }

            return views;
        }

        /// <summary>
        /// Las noticias que salieron desde la última vez que se pidieron (para "Lo que
        /// pasó"), de la más vieja a la más nueva, solo las del país y la ciudad del jugador.
        /// </summary>
        public IReadOnlyList<NewsView> TakeNewNews()
        {
            World world = Session.World;
            Residence? residence = world.Player.Residence;
            int from = Math.Min(_newsRead, world.News.Count);
            _newsRead = world.News.Count;
            return world.News.Skip(from)
                .Where(n => residence != null && n.CountryId == residence.CountryId && (n.CityId == null || n.CityId == residence.CityId))
                .Select(NewsOf)
                .ToList();
        }

        private int _newsRead;

        /// <summary>
        /// La carpeta del juzgado: las causas del jugador, primero las abiertas y después
        /// las que terminaron, de la más nueva a la más vieja.
        /// </summary>
        public IReadOnlyList<CourtCaseView> CourtCases()
        {
            World world = Session.World;
            var views = new List<CourtCaseView>();
            foreach (Courts.CourtCase courtCase in world.CourtCases.Where(c => c.OwnerId == world.PlayerId).OrderByDescending(c => c.IsOpen).ThenByDescending(c => c.Id))
            {
                if (!Session.Content.TryGetCourtCase(courtCase.TypeId, out CourtCaseDefinition? definition))
                {
                    continue;
                }

                var stages = new List<CourtStageView>();
                for (int i = 0; i < definition!.Stages.Count; i++)
                {
                    CourtStageDefinition stage = definition.Stages[i];
                    bool done = !courtCase.IsOpen ? i <= courtCase.StageIndex : i < courtCase.StageIndex;
                    stages.Add(new CourtStageView(stage.Id, T(stage.TextKey(definition.Id)), stage.Weeks, done, courtCase.IsOpen && i == courtCase.StageIndex));
                }

                views.Add(new CourtCaseView(
                    courtCase.TypeId,
                    courtCase.Rit,
                    T(definition.Id),
                    T(definition.CourtKey),
                    DateLabel(courtCase.Opened),
                    courtCase.IsOpen,
                    courtCase.Outcome == null ? null : T(definition.OutcomeTextKey(courtCase.Outcome)),
                    stages,
                    courtCase.IsOpen ? world.Date.WeeksSince(courtCase.StageStarted) : 0));
            }

            return views;
        }

        /// <summary>El celular del jugador: si tiene, sus apps y los correos sin leer.</summary>
        public PhoneView Phone()
        {
            Character player = Session.World.Player;
            int unread = player.Mail?.UnreadCount ?? 0;
            var apps = new List<AppView>();
            foreach (string appId in player.Phone?.Apps ?? (IReadOnlyList<string>)Array.Empty<string>())
            {
                int appUnread = appId == AppView.MailAppId ? unread
                    : appId == Devices.ChatSystem.AppId ? player.Chats?.UnreadCount ?? 0
                    : 0;
                apps.Add(new AppView(appId, T(appId), appUnread));
            }

            return new PhoneView(player.Phone != null, player.Mail != null, unread, apps, player.Chats?.UnreadCount ?? 0);
        }

        /// <summary>Días de un mes de sueldo: un gasto se cuenta como parte de 30 días pagados.</summary>
        public const int PaidDaysPerMonth = 30;

        /// <summary>
        /// Tu dinero (docs/diseno-app.md): lo que tienes, la cartola del último mes y lo que debes.
        /// Solo se lee lo que el núcleo ya cobró y pagó (la liquidación, los gastos del mes y las
        /// cuotas de las deudas); nada se calcula aparte ni se inventa.
        /// </summary>
        public MoneyView Money()
        {
            Character player = Session.World.Player;
            decimal net = player.Job != null && player.LastPayslip != null ? Math.Max(0, player.LastPayslip.Net) : 0;
            decimal perDay = net / PaidDaysPerMonth;
            var lines = new List<MoneyLineView>();
            if (net > 0)
            {
                lines.Add(new MoneyLineView("ui.dinero.sueldo", T("ui.dinero.sueldo"), "+" + MoneyLabel(net), true, string.Empty));
            }

            foreach (PayslipLine line in player.LastExpenses.Where(l => l.Amount < 0))
            {
                // Plata que recibió en el mes (la pensión de alimentos que le pagan).
                lines.Add(new MoneyLineView(line.Id, T(line.Id), "+" + MoneyLabel(-line.Amount), true, string.Empty));
            }

            decimal spent = 0;
            foreach (PayslipLine line in player.LastExpenses)
            {
                if (line.Amount > 0)
                {
                    lines.Add(Outgoing(line.Id, T(line.Id), line.Amount, perDay));
                    spent += line.Amount;
                }
            }

            if (player.LastDebtPayment > 0)
            {
                lines.Add(Outgoing("ui.dinero.cuotas", T("ui.dinero.cuotas"), player.LastDebtPayment, perDay));
                spent += player.LastDebtPayment;
            }

            var debts = player.Debts
                .Where(d => d.Balance > 0)
                .Select(d => new MoneyLineView(d.TypeId, T(d.TypeId), MoneyLabel(d.Balance), false, d.MonthsLate > 0 ? Text("ui.dinero.atrasada", d.MonthsLate) : string.Empty))
                .ToList();

            return new MoneyView(
                MoneyLabel(player.Money),
                lines,
                spent > 0 ? "-" + MoneyLabel(spent) : string.Empty,
                spent > 0 && perDay > 0 ? Text("ui.dinero.mes_en_dias", DaysOf(spent, perDay)) : string.Empty,
                debts);
        }

        private MoneyLineView Outgoing(string id, string label, decimal amount, decimal perDay) =>
            new MoneyLineView(id, label, "-" + MoneyLabel(amount), false, perDay > 0 ? DaysLabel(amount, perDay) : string.Empty);

        /// <summary>"menos de un día de sueldo", "1 día de sueldo", "6 días de sueldo".</summary>
        private string DaysLabel(decimal amount, decimal perDay)
        {
            int days = DaysOf(amount, perDay);
            return days == 0 ? T("ui.dinero.menos_de_un_dia") : days == 1 ? T("ui.dinero.un_dia") : Text("ui.dinero.dias", days);
        }

        private static int DaysOf(decimal amount, decimal perDay) =>
            (int)Math.Round(amount / perDay, 0, MidpointRounding.AwayFromZero);

        /// <summary>
        /// La bandeja de correo del jugador, del más nuevo al más viejo, como mucho
        /// <paramref name="max"/>. Vacía si no tiene correo.
        /// </summary>
        public IReadOnlyList<MailView> Mail(int max = 40)
        {
            Character player = Session.World.Player;
            var views = new List<MailView>();
            if (player.Mail == null)
            {
                return views;
            }

            IReadOnlyList<Devices.MailMessage> messages = player.Mail.Messages;
            for (int i = messages.Count - 1; i >= 0 && views.Count < max; i--)
            {
                Devices.MailMessage message = messages[i];
                MailDefinition? definition = Session.Content.Mail(message.MailId);
                views.Add(new MailView(
                    i,
                    message.MailId,
                    definition == null ? string.Empty : T(definition.Sender),
                    definition == null ? message.MailId : T(definition.SubjectKey),
                    T(message.MailId),
                    definition?.Topic ?? string.Empty,
                    DateLabel(message.Date),
                    AgeLabel(player, message.Date),
                    WhenLabel(Session.World.Date.WeeksSince(message.Date)),
                    message.Read));
            }

            return views;
        }

        /// <summary>El jugador abre un correo (su <see cref="MailView.Index"/>): queda leído.</summary>
        public void ReadMail(int index) => Session.ReadMail(index);

        /// <summary>
        /// La lista de chats de Mensajito, del que tuvo el último mensaje más nuevo al más
        /// viejo. Solo gente real de la vida del jugador; vacía si nunca tuvo la app.
        /// </summary>
        public IReadOnlyList<ChatSummaryView> Chats()
        {
            Character player = Session.World.Player;
            var views = new List<ChatSummaryView>();
            if (player.Chats == null)
            {
                return views;
            }

            foreach (Devices.Chat chat in player.Chats.Chats.Where(c => c.Messages.Count > 0).OrderByDescending(c => c.LastDate.TotalWeeks).ThenBy(c => c.OtherId))
            {
                if (!Session.World.TryGetCharacter(chat.OtherId, out Character? other))
                {
                    continue;
                }

                Devices.ChatMessage last = chat.Messages[chat.Messages.Count - 1];
                views.Add(new ChatSummaryView(
                    other.Id,
                    ContactName(player, other),
                    ContactRelation(player, other),
                    Shorten(T(last.TextKey)),
                    last.FromPlayer,
                    WhenLabel(Session.World.Date.WeeksSince(last.Date)),
                    chat.Unread,
                    chat.AwaitingReply != null,
                    other.Sex == Sex.Female));
            }

            return views;
        }

        /// <summary>La conversación con esa persona, o null si no hay. Abrirla no la marca como vista: para eso, <see cref="ReadChat"/>.</summary>
        public ChatView? Chat(int otherId)
        {
            Character player = Session.World.Player;
            Devices.Chat? chat = player.Chats?.With(otherId);
            if (!Session.World.TryGetCharacter(otherId, out Character? other)
                || (chat == null && !Devices.ChatSystem.Contacts(Session.World, player).Contains(other)))
            {
                return null;
            }

            var bubbles = (chat?.Messages ?? (IReadOnlyList<Devices.ChatMessage>)Array.Empty<Devices.ChatMessage>())
                .Select(m => new ChatBubbleView(T(m.TextKey), m.FromPlayer, WhenLabel(Session.World.Date.WeeksSince(m.Date))))
                .ToList();
            ChatDefinition? awaiting = chat?.AwaitingReply == null ? null : Session.Content.Chat(chat.AwaitingReply);
            bool eventFirst = Session.Pending != null;
            var replies = awaiting == null || !other.IsAlive || eventFirst ? new List<string>() : awaiting.Replies.Select(r => T(r.Id)).ToList();
            var openers = eventFirst
                ? new List<ChatOpenerView>()
                : Devices.ChatSystem.OpenersFor(Session.World, Session.Content, player, other).Select(o => new ChatOpenerView(o.Id, T(o.Id))).ToList();

            // Lo último lo escribió el jugador primero: espera respuesta, o ya lo dejaron en visto.
            Devices.ChatMessage? last = chat == null || chat.Messages.Count == 0 ? null : chat.Messages[chat.Messages.Count - 1];
            bool wrote = last != null && last.FromPlayer && Session.Content.ChatOpener(last.TextKey) != null;
            bool waitingAnswer = wrote && chat!.PlayerWrote != null;
            bool leftOnRead = wrote && chat!.PlayerWrote == null;
            return new ChatView(other.Id, ContactName(player, other), ContactRelation(player, other), other.Sex == Sex.Female, bubbles, replies,
                eventFirst && (awaiting != null || Devices.ChatSystem.OpenersFor(Session.World, Session.Content, player, other).Count > 0),
                openers, waitingAnswer, leftOnRead);
        }

        /// <summary>El jugador abre la conversación: ve todos los mensajes.</summary>
        public void ReadChat(int otherId) => Session.ReadChat(otherId);

        /// <summary>
        /// Las personas a las que el jugador puede escribirle (para "Nuevo chat"): su familia, su
        /// pareja y la gente con la que tiene un lazo. Vacía si no tiene Mensajito.
        /// </summary>
        public IReadOnlyList<ChatContactView> ChatContacts()
        {
            Character player = Session.World.Player;
            if (player.Phone == null || !player.Phone.Has(Devices.ChatSystem.AppId))
            {
                return new List<ChatContactView>();
            }

            return Devices.ChatSystem.Contacts(Session.World, player)
                .Select(p => new ChatContactView(p.Id, ContactName(player, p), ContactRelation(player, p), p.Sex == Sex.Female))
                .ToList();
        }

        /// <summary>El horario del jugador, o null si todavía no tiene (antes de los 6 años).</summary>
        public RoutineView? Routine()
        {
            World world = Session.World;
            Character player = world.Player;
            Life.Routine? routine = player.Routine;
            if (routine == null)
            {
                return null;
            }

            var slots = new List<RoutineSlotView>();
            for (int day = 0; day < Life.Routine.Days; day++)
            {
                foreach (Life.RoutineBlock block in new[] { Life.RoutineBlock.Afternoon, Life.RoutineBlock.Night })
                {
                    string? id = routine.Get(day, block);
                    Content.ActivityDefinition? activity = id == null ? null : Session.Content.Activity(id);
                    bool available = activity == null || Life.RoutineRules.IsAvailable(world, Session.Content, player, activity);
                    slots.Add(new RoutineSlotView(day, block, id, T(id ?? "ui.horario.libre"), available));
                }
            }

            int mood = Life.RoutineRules.MoodTarget(Life.RoutineRules.Total(world, Session.Content, player, routine, "animo")) - player.Stats.Happiness;
            int mind = Life.RoutineRules.MindTarget(Life.RoutineRules.Total(world, Session.Content, player, routine, "mente")) - player.Stats.Smarts;
            int health = Life.RoutineRules.HealthStep(Life.RoutineRules.Total(world, Session.Content, player, routine, "salud"));
            return new RoutineView(slots, routine.Confirmed, Trend(mood), Trend(mind), Trend(health));
        }

        /// <summary>Lo que se puede pegar en ese bloque del horario hoy.</summary>
        public IReadOnlyList<RoutineOptionView> RoutineOptions(Life.RoutineBlock block) =>
            Life.RoutineRules.Options(Session.World, Session.Content, Session.World.Player, block)
                .Select(a => new RoutineOptionView(a.Id, T(a.Id)))
                .ToList();

        /// <summary>Pone una actividad (por su id) en un bloque, o lo deja libre con null.</summary>
        public void SetRoutine(int day, Life.RoutineBlock block, string? activityId) => Session.SetRoutine(day, block, activityId);

        /// <summary>"Así queda mi semestre": la vida sigue con este horario hasta que lo cambie.</summary>
        public void ConfirmRoutine() => Session.ConfirmRoutine();

        private string Trend(int direction) =>
            T(direction > 0 ? "ui.horario.sube" : direction < 0 ? "ui.horario.baja" : "ui.horario.igual");

        /// <summary>El jugador le escribe primero a esa persona uno de <see cref="ChatView.Openers"/> (por su id).</summary>
        public void WriteChat(int otherId, string openerId) => Session.WriteChat(otherId, openerId);

        /// <summary>El jugador contesta con la respuesta número <paramref name="replyIndex"/> de <see cref="ChatView.Replies"/>.</summary>
        public void ReplyChat(int otherId, int replyIndex) => Session.ReplyChat(otherId, replyIndex);

        /// <summary>Cómo tendría guardado el jugador a esa persona: "Mamá" y "Papá" por lo que son; los demás, por su nombre.</summary>
        private string ContactName(Character player, Character other)
        {
            string relation = ContactRelation(player, other);
            return relation == T("ui.relacion.madre") || relation == T("ui.relacion.padre") ? relation : FullName(other);
        }

        /// <summary>Qué es esa persona para el jugador: madre, padre, pareja, hijo o hija, o el lazo que tienen.</summary>
        private string ContactRelation(Character player, Character other)
        {
            if (player.IsChildOf(other.Id))
            {
                return T(other.Sex == Sex.Female ? "ui.relacion.madre" : "ui.relacion.padre");
            }

            if (player.PartnerId == other.Id)
            {
                return T("ui.relacion.pareja");
            }

            if (other.IsChildOf(player.Id))
            {
                return ChildRelation(other);
            }

            string? bond = RelationshipRules.BondKey(player.RelationshipWith(other.Id)?.Bond ?? RelationshipBond.None);
            return bond == null ? string.Empty : T("ui.relacion." + bond);
        }

        /// <summary>Lo corta en <see cref="MailView.PreviewLength"/> letras, en una palabra, con "...".</summary>
        public static string Shorten(string text)
        {
            if (text.Length <= MailView.PreviewLength)
            {
                return text;
            }

            int cut = text.LastIndexOf(' ', MailView.PreviewLength);
            return text.Substring(0, cut > 0 ? cut : MailView.PreviewLength).TrimEnd(',', '.', ';', ':') + "...";
        }

        private NewsView NewsOf(NewsRecord record)
        {
            World world = Session.World;
            string topic = Session.Content.TryGetNews(record.NewsId, out NewsDefinition? definition) ? definition!.Topic : string.Empty;
            return new NewsView(
                record.NewsId,
                T(record.TextKey),
                topic,
                DateLabel(record.Date),
                AgeLabel(world.Player, record.Date),
                world.Date.WeeksSince(record.Date),
                record.CityId != null);
        }

        /// <summary>
        /// Un texto de la pantalla (botones, títulos) en el idioma del jugador.
        /// Los {0}, {1}... se reemplazan por los valores dados.
        /// </summary>
        public string Text(string key, params object[] values) =>
            values.Length == 0 ? T(key) : string.Format(CultureInfo.InvariantCulture, T(key), values);

        /// <summary>
        /// Resume lo que pasó entre dos fotos de la misma vida: el tiempo que corrió y los
        /// cambios de salud, felicidad, inteligencia y dinero. Si las fotos son de vidas
        /// distintas (se siguió con un hijo), no hay nada que comparar y el resumen sale vacío.
        /// </summary>
        public StepSummary Summarize(LifeSnapshot before, LifeSnapshot after, int weeks)
        {
            if (before == null)
            {
                throw new ArgumentNullException(nameof(before));
            }

            if (after == null)
            {
                throw new ArgumentNullException(nameof(after));
            }

            if (before.Generation != after.Generation || before.Seed != after.Seed)
            {
                return new StepSummary(0, string.Empty, new ChangeView[0]);
            }

            var changes = new List<ChangeView>();
            AddChange(changes, "ui.salud", after.Health - before.Health);
            AddChange(changes, "ui.felicidad", after.Happiness - before.Happiness);
            AddChange(changes, "ui.inteligencia", after.Smarts - before.Smarts);
            decimal money = Math.Round(after.Money - before.Money, 0, MidpointRounding.AwayFromZero);
            if (Math.Abs(money) >= MinVisibleMoneyChange)
            {
                string sign = money > 0 ? "+" : "-";
                changes.Add(new ChangeView($"{T("ui.dinero_titulo")} {sign}{MoneyLabel(Math.Abs(money))}", money > 0));
            }

            return new StepSummary(Math.Max(0, weeks), ElapsedLabel(weeks), changes);
        }

        /// <summary>"Pasó una semana", "Pasaron 3 semanas", "Pasó 1 año y 4 semanas"... Vacío si no pasó tiempo.</summary>
        public string ElapsedLabel(int weeks)
        {
            if (weeks <= 0)
            {
                return string.Empty;
            }

            int years = weeks / GameDate.WeeksPerYear;
            int rest = weeks % GameDate.WeeksPerYear;
            if (years == 0)
            {
                return weeks == 1 ? T("ui.paso.una_semana") : Text("ui.paso.semanas", weeks);
            }

            if (rest == 0)
            {
                return years == 1 ? T("ui.paso.un_ano") : Text("ui.paso.anos", years);
            }

            return years == 1 ? Text("ui.paso.un_ano_semanas", years, rest) : Text("ui.paso.anos_semanas", years, rest);
        }

        /// <summary>
        /// Cómo nace la vida de esta partida (para elegir entre varios nacimientos antes de empezar).
        /// Tiene sentido al principio, pero se puede pedir en cualquier momento.
        /// </summary>
        public BirthView Birth()
        {
            Character player = Session.World.Player;
            var parents = new List<PersonView>();
            if (player.MotherId.HasValue)
            {
                parents.Add(Person(Session.World.GetCharacter(player.MotherId.Value), T("ui.relacion.madre")));
            }

            if (player.FatherId.HasValue)
            {
                parents.Add(Person(Session.World.GetCharacter(player.FatherId.Value), T("ui.relacion.padre")));
            }

            bool female = player.Sex == Sex.Female;
            return new BirthView(
                FullName(player),
                female,
                T(female ? "ui.nacer.nina" : "ui.nacer.nino"),
                Place(player),
                T(HomeLevelKey(player.HouseholdLevel)),
                parents);
        }

        /// <summary>El hogar en una frase, según su nivel (0 a 100).</summary>
        public static string HomeLevelKey(int householdLevel) =>
            householdLevel >= WealthyHomeLevel ? "ui.hogar.acomodado"
            : householdLevel >= MiddleHomeLevel ? "ui.hogar.medio"
            : "ui.hogar.justo";

        private void AddChange(List<ChangeView> changes, string titleKey, int delta)
        {
            if (delta != 0)
            {
                string amount = delta > 0 ? "+" + delta.ToString(CultureInfo.InvariantCulture) : delta.ToString(CultureInfo.InvariantCulture);
                changes.Add(new ChangeView($"{T(titleKey)} {amount}", delta > 0));
            }
        }

        private int Advance(int maxWeeks)
        {
            LifePhase phase = Phase;
            if (phase != LifePhase.Living)
            {
                return 0;
            }

            int year = Session.World.Date.Year;
            int playerId = Session.World.PlayerId;
            int weeks = Session.Advance(maxWeeks);
            if (Session.World.Date.Year > year && Session.World.PlayerId == playerId)
            {
                _closedChapter = Chapter(Session.World.Date.Year - 1);
            }

            return weeks;
        }

        private ChapterView? _closedChapter;

        /// <summary>
        /// El capítulo del año que se acaba de cerrar, una sola vez: la pantalla lo muestra al
        /// pasar de año (después vive en <see cref="Chapters"/>). Null si no se cerró ninguno.
        /// </summary>
        public ChapterView? TakeClosedChapter()
        {
            ChapterView? chapter = _closedChapter;
            _closedChapter = null;
            return chapter;
        }

        /// <summary>
        /// Un año como capítulo. El título lo da la línea del diario de más peso de ese año
        /// (chapters/ en los paquetes; si empatan, la más nueva). Un título (su texto) sale una sola vez
        /// por vida: si un año anterior ya lo usó, gana la siguiente línea. Si ninguna da
        /// título, el año se llama por la edad ("El año en que cumplí 9").
        /// </summary>
        public ChapterView Chapter(int year)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            World world = Session.World;
            int first = world.Player.BirthDate.Year;
            for (int y = first; y < year; y++)
            {
                string? title = TitleText(BestTitle(y, used, out _, out _));
                if (title != null)
                {
                    used.Add(title);
                }
            }

            return ChapterWith(year, used);
        }

        /// <summary>
        /// El diario por capítulos: un capítulo por cada año vivido en que pasó algo, del
        /// primero al de ahora (el último puede estar abierto todavía).
        /// </summary>
        public IReadOnlyList<ChapterView> Chapters()
        {
            World world = Session.World;
            var years = new HashSet<int>(world.Journal.Where(e => e.CharacterId == world.PlayerId).Select(e => e.Date.Year));
            var used = new HashSet<string>(StringComparer.Ordinal);
            var chapters = new List<ChapterView>();
            for (int y = world.Player.BirthDate.Year; y <= world.Date.Year; y++)
            {
                if (years.Contains(y))
                {
                    chapters.Add(ChapterWith(y, used));
                }
                else
                {
                    string? title = TitleText(BestTitle(y, used, out _, out _));
                    if (title != null)
                    {
                        used.Add(title);
                    }
                }
            }

            return chapters;
        }

        /// <summary>Arma el capítulo de ese año sin repetir los títulos ya usados (y suma el suyo).</summary>
        private ChapterView ChapterWith(int year, HashSet<string> used)
        {
            World world = Session.World;
            Character player = world.Player;
            GameDate end = new GameDate((year * GameDate.WeeksPerYear) - 1);
            int age = player.AgeInYears(end);
            ChapterTitleDefinition? title = BestTitle(year, used, out JournalEntry? best, out Character? person);
            if (title != null)
            {
                used.Add(T(title.TitleKey));
            }

            string text = title != null
                ? WithPerson(T(title.TitleKey), person?.FirstName)
                : age == 0 ? T("ui.capitulo.nacer") : Text("ui.capitulo.cumpli", age);
            var months = world.Journal
                .Where(e => e.CharacterId == player.Id && e.Date.Year == year)
                .GroupBy(e => e.Date.Month)
                .OrderBy(g => g.Key)
                .Select(g => new ChapterMonthView(T("ui.mes." + g.Key), g.Select(e => T(e.TextKey)).ToList()))
                .ToList();
            return new ChapterView(year, text, AgeLabel(player, end), months, best == null ? null : PhotoOf(best, person), year == world.Date.Year);
        }

        /// <summary>El texto de un título sin la persona: dos líneas que titulan igual cuentan como el mismo título.</summary>
        private string? TitleText(ChapterTitleDefinition? title) => title == null ? null : T(title.TitleKey);

        /// <summary>
        /// La línea del diario que titula el año: la de más peso cuyo título no se haya usado
        /// (si empatan, la más nueva). Null si ninguna da título.
        /// </summary>
        private ChapterTitleDefinition? BestTitle(int year, HashSet<string> used, out JournalEntry? best, out Character? bestPerson)
        {
            World world = Session.World;
            best = null;
            bestPerson = null;
            ChapterTitleDefinition? bestTitle = null;
            foreach (JournalEntry entry in world.Journal.Where(e => e.CharacterId == world.PlayerId && e.Date.Year == year))
            {
                ChapterTitleDefinition? title = Session.Content.ChapterTitleFor(entry.TextKey);
                if (title == null || used.Contains(T(title.TitleKey)) || (bestTitle != null && title.Weight < bestTitle.Weight))
                {
                    continue;
                }

                Character? person = PersonOnThatDay(entry.Date);
                if (person == null && T(title.TitleKey).Contains(PersonMark))
                {
                    continue; // el título nombra a alguien que no sabemos quién es
                }

                best = entry;
                bestTitle = title;
                bestPerson = person;
            }

            return bestTitle;
        }

        /// <summary>
        /// La persona de un día: la del momento que plantó una semilla ese día (la que quedó
        /// ligada a lo que hiciste). Null si ese día no dejó a nadie.
        /// </summary>
        private Character? PersonOnThatDay(GameDate date)
        {
            World world = Session.World;
            foreach (Seed seed in world.Seeds.All.Where(s => s.OwnerId == world.PlayerId && s.PlantedDate == date && s.Origin != null))
            {
                int? id = seed.Origin!.PersonId ?? (seed.Witnesses.Count > 0 ? seed.Witnesses.First() : (int?)null);
                if (id.HasValue && world.TryGetCharacter(id.Value, out Character? person))
                {
                    return person;
                }
            }

            return null;
        }

        /// <summary>La foto del capítulo: el día de esa línea y, si se sabe, dónde y con quién.</summary>
        private ChapterPhotoView PhotoOf(JournalEntry entry, Character? person)
        {
            World world = Session.World;
            string? placeKey = world.Seeds.All
                .Where(s => s.OwnerId == world.PlayerId && s.PlantedDate == entry.Date && s.Origin?.PlaceKey != null)
                .Select(s => s.Origin!.PlaceKey)
                .FirstOrDefault();
            if (placeKey == null)
            {
                // Si ese día pasó un solo momento, su lugar.
                var records = world.EventHistory.Where(r => r.CharacterId == world.PlayerId && r.Date == entry.Date).ToList();
                if (records.Count == 1 && Session.Content.TryGetEvent(records[0].EventId, out EventDefinition? definition))
                {
                    placeKey = definition?.Place;
                }
            }

            string when = string.Format(CultureInfo.InvariantCulture, T("ui.fecha.dia"), entry.Date.DayOfMonth, T("ui.mes." + entry.Date.Month));
            return new ChapterPhotoView(when, placeKey == null ? null : T(placeKey), person?.FirstName, person?.Id);
        }

        private string T(string key) => Session.Text(key, Language);

        private JournalLine Line(JournalEntry entry) => new JournalLine(
            AgeLabel(Session.World.Player, entry.Date),
            T(entry.TextKey),
            string.Format(CultureInfo.InvariantCulture, T("ui.fecha.dia"), entry.Date.DayOfMonth, T("ui.mes." + entry.Date.Month)));

        private EventView EventOf(PendingEvent pending)
        {
            EventDefinition definition = Session.Content.GetEvent(pending.EventId);
            Character? person = EventSystem.PersonOf(Session.World, pending);
            string? personName = person == null ? null : PersonName(definition, person);
            var options = pending.AvailableOptions.Select(i => WithTest(WithPerson(T(definition.Options[i].TextKey), personName))).ToList();
            int? previous = Session.TimeMachine.PreviousChoice(pending);
            int position = previous.HasValue ? IndexOf(pending.AvailableOptions, previous.Value) : -1;
            string textKey = definition.TextKeyFor(Session.World.TimesHappened(definition, pending.CharacterId, pending.PersonId));

            // Quién mira y quién lo va a notar: los testigos de las semillas, sin aplicar nada.
            World world = Session.World;
            Seed? seed = pending.SeedId.HasValue ? world.Seeds.Get(pending.SeedId.Value) : null;
            var scope = new EventScope(world, Session.Content, world.GetCharacter(pending.CharacterId), seed, readOnly: true);
            var effects = pending.AvailableOptions.Select(i => definition.Options[i].CompiledEffects).ToList();
            bool remembered = effects.Any(list => list.Any(e => e.PlantsSeed));
            var noticedBy = effects.Select(list => Names(list.SelectMany(e => e.Witnesses(scope))
                    .Where(v => v.Kind == ValueKind.Object)
                    .Select(v => (v.AsObject("testigo") as CharacterObject)?.Character)))
                .Select(names => names.Length == 0 ? string.Empty : Text("ui.evento.lo_nota", names))
                .ToList();
            string watchers = seed == null ? string.Empty : Names(seed.Witnesses.Select(id => world.TryGetCharacter(id, out Character? c) ? c : null));
            string? watching = watchers.Length == 0 ? null : Text("ui.evento.miran", watchers);

            return new EventView(pending.EventId, WithTest(WithPerson(T(textKey), personName)), options, position >= 0 ? position : (int?)null, definition.KeyDay, remembered, watching, noticedBy)
            {
                Person = person == null ? null : PersonInMoment(definition, person),
                PersonId = person?.Id,
                Place = definition.Place == null ? null : T(definition.Place),
                AllowsRule = definition.AllowsOwnRule,
                Origin = seed == null ? null : OriginOf(seed),
            };
        }

        /// <summary>
        /// "Esto viene de acá": el día en que nació la semilla que hoy despierta. Null si la
        /// semilla no sabe de dónde viene (partidas viejas, o la plantó una regla y no un momento).
        /// </summary>
        private SeedOriginView? OriginOf(Seed seed)
        {
            SeedOrigin? origin = seed.Origin;
            if (origin == null || !Session.Content.TryGetEvent(origin.EventId, out EventDefinition? definition) || definition == null)
            {
                return null;
            }

            World world = Session.World;
            // La persona de ese día: la del momento o, si el momento no tenía, la que quedó
            // ligada a la semilla (el amigo al que le prestaste plata).
            int? personId = origin.PersonId ?? (seed.Witnesses.Count > 0 ? seed.Witnesses.First() : (int?)null);
            Character? person = personId.HasValue && world.TryGetCharacter(personId.Value, out Character? c) ? c : null;
            string? personName = person == null ? null : PersonName(definition, person);
            string moment = WithPerson(T(definition.ResolvedTextKey), personName);
            string? choice = origin.OptionTextKey == null ? null : WithPerson(T(origin.OptionTextKey), personName);
            string when = string.Format(CultureInfo.InvariantCulture, T("ui.fecha.otro_ano"),
                string.Format(CultureInfo.InvariantCulture, T("ui.fecha.dia"), seed.PlantedDate.DayOfMonth, T("ui.mes." + seed.PlantedDate.Month)),
                AgeLabel(world.Player, seed.PlantedDate));

            // A qué foto se puede volver para cambiarlo: la última sacada antes de ese día
            // (la de esa misma decisión, si era día clave, o el comienzo de esa etapa).
            TimeMachine machine = Session.TimeMachine;
            WorldSnapshot? snapshot = machine.Snapshots
                .Where(s => s.PlayerId == world.PlayerId && s.Date <= seed.PlantedDate && machine.CanGoBackTo(s))
                .OrderBy(s => s.Date)
                .LastOrDefault();

            return new SeedOriginView(
                T("ui.origen.titulo"),
                when,
                origin.PlaceKey == null ? null : T(origin.PlaceKey),
                person == null ? null : PersonInMoment(definition, person),
                WithTest(moment),
                choice == null ? null : Text("ui.origen.elegiste", choice),
                snapshot?.Id,
                snapshot == null ? null : Text("ui.origen.volver", snapshot.PlayerAgeInYears));
        }

        /// <summary>
        /// Vuelve al pasado justo frente al error: a la foto que ofrece
        /// <see cref="SeedOriginView.SnapshotId"/> del momento que se está viendo. Gasta una carga.
        /// </summary>
        public void GoBackToOrigin()
        {
            int? snapshotId = Session.Pending == null ? null : EventOf(Session.Pending).Origin?.SnapshotId;
            if (!snapshotId.HasValue)
            {
                throw new InvalidOperationException("Este momento no tiene un día al que volver.");
            }

            Session.GoBack(snapshotId.Value);
        }

        /// <summary>
        /// Cómo se nombra a la persona del momento: a la mamá y al papá se les dice
        /// "tu mamá" y "tu papá" (también cuando van como apoderado); a los demás, por su nombre.
        /// </summary>
        private string PersonInMoment(EventDefinition definition, Character person) =>
            definition.With == "madre" || definition.With == "padre" ? T("ui.momento." + definition.With)
            : definition.With == EventDefinition.WitnessRole && Session.World.Player.MotherId == person.Id ? T("ui.momento.madre")
            : definition.With == EventDefinition.WitnessRole && Session.World.Player.FatherId == person.Id ? T("ui.momento.padre")
            : PersonName(definition, person);

        /// <summary>
        /// El nombre con que el texto de un momento nombra a la persona: a quien hace clases
        /// se le dice "la profe Carolina" o "el profe Juan"; a los demás, por su nombre.
        /// </summary>
        private string PersonName(EventDefinition definition, Character person) =>
            definition.With == "jefe" && Work.OccupationRules.CareerOf(Session.Content, Session.World.Player.Job)?.Occupation!.BossKey is string bossKey
                ? Text(bossKey + ".momento" + (person.Gender == Gender.Female ? ".f" : ".m"), person.FirstName)
            : definition.With == "profe" || definition.With == "jefe"
                ? Text("ui.momento." + definition.With + (person.Gender == Gender.Female ? ".f" : ".m"), person.FirstName)
                : definition.With == "apoderado"
                    ? T(person.Sex == Sex.Female ? "ui.momento.madre" : "ui.momento.padre")
                    : person.FirstName;

        /// <summary>
        /// La línea de con quién y dónde pasa el momento ("Con tu mamá · Tu casa"); null si no dice ninguna.
        /// </summary>
        public string? MomentLine(EventView view)
        {
            var parts = new List<string>();
            if (view.Person != null)
            {
                parts.Add(Text("ui.momento.con", view.Person));
            }

            if (view.Place != null)
            {
                parts.Add(view.Place);
            }

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        /// <summary>Marca de los textos que se cambia por el ramo de la prueba anunciada o devuelta ("Matemática").</summary>
        public const string SubjectMark = "{ramo}";

        /// <summary>Marca de los textos de la búsqueda que se cambia por la empresa que te llamó ("Moda Cordillera").</summary>
        public const string CompanyMark = "{empresa}";

        /// <summary>Marca de los textos que se cambia por la nota de esa prueba ("5,8").</summary>
        public const string MarkMark = "{nota}";

        /// <summary>Cambia {ramo} y {nota} por los de la prueba del jugador (la anunciada o la que se devolvió).</summary>
        private string WithTest(string text)
        {
            if (text.Contains(CompanyMark))
            {
                string? company = Session.World.Player.JobSearch?.CompanyNameId;
                text = text.Replace(CompanyMark, company != null ? T(company) : T("ui.busqueda.una_empresa"));
            }

            Education.SchoolRecord? record = Session.World.Player.SchoolRecord;
            if (record?.TestSubjectId == null)
            {
                return text;
            }

            text = text.Replace(SubjectMark, T(record.TestSubjectId));
            return record.TestMark.HasValue ? text.Replace(MarkMark, MarkText(record.TestMark.Value)) : text;
        }

        /// <summary>Una nota como se escribe en el país ("5,8" en Chile, "7" en Argentina).</summary>
        public string MarkText(double mark)
        {
            int decimals = Session.World.Player.Residence == null ? 1
                : Session.Content.GetCountry(Session.World.Player.Residence.CountryId).GradeScale?.Decimals ?? 1;
            return mark.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Replace('.', ',');
        }

        /// <summary>Marca de los textos que se cambia por el nombre de la persona del momento.</summary>
        public const string PersonMark = "{persona}";

        private static string WithPerson(string text, Character? person) => WithPerson(text, person?.FirstName);

        private static string WithPerson(string text, string? name) =>
            name == null ? text : text.Replace(PersonMark, name);

        /// <summary>Los nombres de pila de las personas vivas, en una lista ("Pedro, Ana y Luis"); vacío si no hay.</summary>
        private string Names(IEnumerable<Character?> people)
        {
            var names = people
                .Where(p => p != null && p.IsAlive && p.Id != Session.World.Player.Id)
                .Select(p => p!.FirstName)
                .Distinct()
                .ToList();
            if (names.Count <= 1)
            {
                return names.Count == 0 ? string.Empty : names[0];
            }

            return string.Join(", ", names.Take(names.Count - 1)) + T("ui.lista.y") + names[names.Count - 1];
        }

        private string MomentLabel(WorldSnapshot snapshot) => snapshot.Kind == SnapshotKind.StageStart
            ? Text("ui.pasado.etapa", snapshot.PlayerAgeInYears, T(snapshot.StageId))
            : Text("ui.pasado.decision", snapshot.PlayerAgeInYears, T(Session.Content.GetEvent(snapshot.EventId!).ResolvedTextKey));

        private static int IndexOf(IReadOnlyList<int> list, int value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Hace cuánto pasó algo, contado en semanas: "Esta semana", "Hace 3 semanas", "Hace 2 meses", "Hace un año".</summary>
        public string WhenLabel(int weeks)
        {
            if (weeks <= 0)
            {
                return T("ui.hace.esta_semana");
            }

            if (weeks == 1)
            {
                return T("ui.hace.semana_pasada");
            }

            if (weeks < 9)
            {
                return string.Format(CultureInfo.InvariantCulture, T("ui.hace.semanas"), weeks);
            }

            int years = weeks / GameDate.WeeksPerYear;
            if (years == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, T("ui.hace.meses"), Math.Max(2, weeks * 12 / GameDate.WeeksPerYear));
            }

            return years == 1 ? T("ui.hace.un_ano") : string.Format(CultureInfo.InvariantCulture, T("ui.hace.anos"), years);
        }

        /// <summary>El día de hoy para la cabecera: "Lunes 12 de marzo" (cada semana empieza un lunes).</summary>
        private string Today(GameDate date) =>
            string.Format(CultureInfo.InvariantCulture, T("ui.fecha.hoy"), date.DayOfMonth, T("ui.mes." + date.Month));

        /// <summary>
        /// Una fecha pasada como la diría una persona: "12 de marzo" si es de este año
        /// y "12 de marzo · 13 años" (la edad que tenías) si es de otro. Nunca "año 103, semana 10".
        /// </summary>
        private string DateLabel(GameDate date)
        {
            string day = string.Format(CultureInfo.InvariantCulture, T("ui.fecha.dia"), date.DayOfMonth, T("ui.mes." + date.Month));
            World world = Session.World;
            if (date.Year == world.Date.Year || date < world.Player.BirthDate)
            {
                return day;
            }

            return string.Format(CultureInfo.InvariantCulture, T("ui.fecha.otro_ano"), day, AgeLabel(world.Player, date));
        }

        private string AgeLabel(Character character, GameDate date)
        {
            int weeks = character.AgeInWeeks(date);
            int years = weeks / GameDate.WeeksPerYear;
            if (years == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, T("ui.edad.semanas"), weeks);
            }

            return years == 1 ? T("ui.edad.un_ano") : string.Format(CultureInfo.InvariantCulture, T("ui.edad.anos"), years);
        }

        private string Place(Character player)
        {
            Residence? residence = player.Residence;
            return residence == null ? string.Empty : $"{T(residence.CommuneId)}, {T(residence.CityId)}, {T(residence.CountryId)}";
        }

        private string MoneyLabel(decimal money)
        {
            var format = new NumberFormatInfo { NumberGroupSeparator = T("ui.separador_miles"), NumberDecimalDigits = 0 };
            string amount = Math.Round(money, 0, MidpointRounding.AwayFromZero).ToString("N0", format);
            return string.Format(CultureInfo.InvariantCulture, T("ui.dinero"), amount);
        }

        private IReadOnlyList<PersonView> Family(Character player)
        {
            World world = Session.World;
            var family = new List<PersonView>();
            if (player.MotherId.HasValue)
            {
                family.Add(Person(world.GetCharacter(player.MotherId.Value), T("ui.relacion.madre")));
            }

            if (player.FatherId.HasValue)
            {
                family.Add(Person(world.GetCharacter(player.FatherId.Value), T("ui.relacion.padre")));
            }

            if (player.PartnerId.HasValue)
            {
                family.Add(Person(world.GetCharacter(player.PartnerId.Value), T("ui.relacion.pareja")));
            }

            family.AddRange(world.ChildrenOf(player.Id).Select(c => Person(c, ChildRelation(c))));
            return family;
        }

        /// <summary>
        /// Quiénes están en la casa y cuál casa es: el jugador, su pareja, sus hijos
        /// menores de edad y, mientras no viva por su cuenta (no arrienda ni se fue de la
        /// casa en un evento), sus padres. Si arrienda, la casa es lo que arrendó.
        /// </summary>
        private HomeView Home(Character player)
        {
            World world = Session.World;
            int age = player.AgeInYears(world.Date);
            var members = new List<HomeMemberView> { Member(player, string.Empty, isPlayer: true) };

            if (!player.HasFlag(HousingRules.LivesOnOwnFlag))
            {
                AddIfAlive(members, player.MotherId, T("ui.relacion.madre"));
                AddIfAlive(members, player.FatherId, T("ui.relacion.padre"));
            }

            AddIfAlive(members, player.PartnerId, T("ui.relacion.pareja"));
            foreach (Character child in CustodyRules.MinorChildren(world, player).Where(c => CustodyRules.LivesWith(world, c, player)))
            {
                members.Add(Member(child, ChildRelation(child), isPlayer: false));
            }

            return new HomeView(player.Residence?.CommuneId ?? string.Empty, player.HouseholdLevel, StageOf(age), members, SceneObjects(player), player.Lease?.TypeId);
        }

        /// <summary>
        /// Los objetos de la escena (paquetes, carpeta scene/) cuya condición se cumple ahora
        /// para el jugador: así la casa muestra lo que está viviendo y lo que decidió.
        /// </summary>
        public IReadOnlyList<string> SceneObjects(Character player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var scope = new EventScope(Session.World, Session.Content, player, null, readOnly: true);
            return Session.Content.SceneObjects
                .Where(o => o.CompiledConditions != null && o.CompiledConditions.IsTrue(scope))
                .Select(o => o.Id)
                .ToList();
        }

        /// <summary>
        /// El lugar de trabajo sale del título que mejor paga (el mismo que usa el sueldo);
        /// sin título, trabaja en un local de atención.
        /// </summary>
        private WorkplaceView? Workplace(Character player)
        {
            if (player.Job == null)
            {
                return null;
            }

            HomeMemberView figure = Member(player, string.Empty, isPlayer: true);
            bool freelance = player.Job.Kind == JobKind.Freelance;
            Company? company = Session.World.CompanyById(player.Job.CompanyId);
            string? sizeId = company != null && company.IsOpen ? company.SizeId : null;
            string? companyText = sizeId == null ? null
                : company!.NameId != null ? Text("ui.trabajo.empresa", T(company.NameId), T(sizeId))
                : Text("ui.trabajo.empresa_sin_nombre", T(sizeId));
            if (player.Degrees.Count == 0 && player.Job.Position == null)
            {
                return new WorkplaceView(null, null, 1, freelance, figure, 0, companyText, sizeId);
            }

            double bestRelative = player.Degrees.Count == 0 ? 1 : player.Degrees.Max(d => Session.Content.GetCareerLevel(d).SalaryRelative);
            if (player.Degrees.Count == 0)
            {
                // Sin título, con cargo: trabaja en un oficio.
                return OccupationWorkplace(player, figure, freelance, companyText, sizeId, bestRelative);
            }

            CareerLevelDefinition best = player.Degrees
                .Select(d => Session.Content.GetCareerLevel(d))
                .OrderByDescending(l => l.SalaryRelative)
                .First();
            CareerPosition? position = player.Job.Position;
            if (position == null)
            {
                CareerDefinition career = Session.Content.GetCareer(best.CareerId);
                return new WorkplaceView(career.Id, career.WorkplaceKind, best.SalaryRelative, freelance, figure, 0, companyText, sizeId);
            }

            // Con cargo, manda la carrera del cargo (puede trabajar en algo distinto a su mejor título).
            return OccupationWorkplace(player, figure, freelance, companyText, sizeId, best.SalaryRelative);
        }

        /// <summary>El lugar de trabajo de quien tiene un cargo: el de la carrera o el oficio de ese cargo.</summary>
        private WorkplaceView OccupationWorkplace(Character player, HomeMemberView figure, bool freelance, string? companyText, string? sizeId, double salaryRelative)
        {
            CareerPosition position = player.Job!.Position!;
            CareerPositionDefinition current = Session.Content.GetCareerPosition(position.PositionId);
            CareerDefinition jobCareer = Session.Content.GetCareer(current.CareerId);
            string caption = Text("ui.trabajo.cargo", T(position.PositionId), T(position.AreaId), player.Job.Performance);
            if (jobCareer.IsOccupation && player.Job.GoalPercent > 0)
            {
                caption += " · " + Text("ui.oficio.meta", player.Job.GoalPercent);
            }

            if (companyText != null)
            {
                caption = companyText + " · " + caption;
            }

            return new WorkplaceView(jobCareer.Id, jobCareer.WorkplaceKind, salaryRelative, freelance, figure, RankOf(jobCareer, current), caption, sizeId);
        }

        /// <summary>
        /// Cuántos ascensos hay desde el primer cargo de la carrera hasta este
        /// (el camino más corto, porque la escalera se puede dividir).
        /// </summary>
        public static int RankOf(CareerDefinition career, CareerPositionDefinition position)
        {
            if (career.Positions.Count == 0)
            {
                return 0;
            }

            var depth = new Dictionary<string, int> { [career.Positions[0].Id] = 0 };
            var pending = new Queue<CareerPositionDefinition>();
            pending.Enqueue(career.Positions[0]);
            while (pending.Count > 0)
            {
                CareerPositionDefinition step = pending.Dequeue();
                foreach (CareerPositionDefinition next in step.NextPositions)
                {
                    if (!depth.ContainsKey(next.Id))
                    {
                        depth[next.Id] = depth[step.Id] + 1;
                        pending.Enqueue(next);
                    }
                }
            }

            return depth.TryGetValue(position.Id, out int rank) ? rank : 0;
        }

        private void AddIfAlive(List<HomeMemberView> members, int? id, string relation)
        {
            if (id.HasValue)
            {
                Character person = Session.World.GetCharacter(id.Value);
                if (person.IsAlive)
                {
                    members.Add(Member(person, relation, isPlayer: false));
                }
            }
        }

        private HomeMemberView Member(Character character, string relation, bool isPlayer)
        {
            int age = character.AgeInYears(Session.World.Date);
            return new HomeMemberView(character.Id, FullName(character), relation, age, StageOf(age), character.Sex == Sex.Female, isPlayer, character.Stats.Happiness);
        }

        /// <summary>
        /// Amistades, colegas, rivales y ex vivos, ordenados por lo fuerte que es el lazo
        /// visto desde la otra persona (el rival por su rencor, el resto por su cariño).
        /// </summary>
        private IReadOnlyList<PersonView> People(Character player)
        {
            World world = Session.World;
            var bonds = new[] { RelationshipBond.Friend, RelationshipBond.Ex, RelationshipBond.Colleague, RelationshipBond.Rival };
            var people = new List<(PersonView View, int Strength)>();
            foreach (RelationshipBond bond in bonds)
            {
                string relation = T("ui.relacion." + RelationshipRules.BondKey(bond));
                foreach (Character other in RelationshipRules.WithBond(world, player, bond))
                {
                    Relationship feelings = RelationshipRules.Feelings(world, other, player);
                    int strength = bond == RelationshipBond.Rival ? feelings.Resentment : feelings.Affection;
                    people.Add((Person(other, relation), strength));
                }
            }

            // OrderByDescending es estable: con la misma fuerza, se mantiene el orden de los lazos y de los ids.
            return people.OrderByDescending(p => p.Strength).Take(MaxPeople).Select(p => p.View).ToList();
        }

        /// <summary>
        /// La relación en una palabra, según lo que la otra persona siente por el jugador:
        /// el rencor fuerte pesa más que el cariño; si no, manda el cariño.
        /// </summary>
        public static string FeelingKey(Relationship towardPlayer)
        {
            if (towardPlayer.Resentment >= VisibleResentment)
            {
                return "ui.sentir.rencor";
            }

            int affection = towardPlayer.Affection;
            return affection >= 75 ? "ui.sentir.muy_unidos"
                : affection >= 55 ? "ui.sentir.buena"
                : affection >= 35 ? "ui.sentir.tibia"
                : "ui.sentir.distanciados";
        }

        /// <summary>Los tres estados que la pantalla muestra en palabras, sin números.</summary>
        public static readonly string[] States = { "salud", "animo", "mente" };

        /// <summary>
        /// Un estado del jugador (salud, ánimo o mente) en palabras, sin números
        /// (docs/diseno-app.md, "Pantalla de la vida"): "salud" usa la salud, "animo" la
        /// felicidad y "mente" la inteligencia.
        /// </summary>
        public string StateWord(string state, int value) => T(StateWordKey(state, value));

        /// <summary>La clave del texto de un estado: cinco tramos de 20 puntos, de 0 (lo peor) a 4 (lo mejor).</summary>
        public static string StateWordKey(string state, int value)
        {
            int band = value >= 80 ? 4 : value >= 60 ? 3 : value >= 40 ? 2 : value >= 20 ? 1 : 0;
            return $"ui.estado.{state}.{band}";
        }

        /// <summary>Desde cuánto rencor (0 a 100) se nota en el trato; un rival recién conocido ya lo muestra.</summary>
        public const int VisibleResentment = 40;

        private string ChildRelation(Character child) => T(child.Sex == Sex.Female ? "ui.relacion.hija" : "ui.relacion.hijo");

        private PersonView Person(Character character, string relation)
        {
            World world = Session.World;
            GameDate at = character.DeathDate ?? world.Date;
            string feeling = character.IsAlive && character.Id != world.Player.Id
                ? T(FeelingKey(RelationshipRules.Feelings(world, character, world.Player)))
                : string.Empty;
            return new PersonView(character.Id, FullName(character), relation, character.AgeInYears(at), character.IsAlive, feeling);
        }

        private static string FullName(Character character) => $"{character.FirstName} {character.LastName}";
    }
}

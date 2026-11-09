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
            "ui.edad.semanas", "ui.edad.un_ano", "ui.edad.anos", "ui.fecha", "ui.dinero", "ui.separador_miles",
            "ui.relacion.madre", "ui.relacion.padre", "ui.relacion.pareja", "ui.relacion.hija", "ui.relacion.hijo",
            "ui.boton.seguir_viviendo", "ui.boton.seguir_con", "ui.boton.nueva_vida", "ui.salud", "ui.felicidad", "ui.inteligencia",
            "ui.dinero_titulo", "ui.diario", "ui.familia", "ui.generacion", "ui.partida", "ui.legado", "ui.fin", "ui.fallecido",
            "ui.vista.casa", "ui.vista.barrio", "ui.vista.trabajo", "ui.trabajo.cargo", "ui.lugar.casa", "ui.lugar.colegio", "ui.lugar.estudios", "ui.lugar.trabajo",
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

        /// <summary>
        /// Edad hasta la que, sin pareja, se dibuja al jugador viviendo con sus padres.
        /// En Chile la mayoría de los jóvenes se independiza cerca de los 28 años (CASEN).
        /// Es solo para dibujar: cuando exista el sistema de vivienda, lo decidirá el núcleo.
        /// </summary>
        public const int LeavesHomeAge = 28;

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
                string.Format(CultureInfo.InvariantCulture, T("ui.fecha"), world.Date.Year, world.Date.WeekOfYear),
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

        /// <summary>Avanza una sola semana (o nada, si hay que decidir algo primero).</summary>
        public int AdvanceWeek() => Advance(1);

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
        /// <summary>
        /// "El mes" de Tu dinero: los gastos del último mes contados en días de trabajo
        /// (el sueldo líquido del mes repartido en <see cref="MonthView.WorkDaysPerMonth"/> días).
        /// </summary>
        public MonthView Month()
        {
            Character player = Session.World.Player;
            decimal net = player.Job != null && player.LastPayslip != null ? Math.Max(0m, player.LastPayslip.Net) : 0m;
            decimal dayPay = net / MonthView.WorkDaysPerMonth;
            bool hasPay = dayPay > 0;

            var expenses = player.LastExpenses
                .OrderByDescending(l => l.Amount)
                .Select(l =>
                {
                    double days = hasPay ? (double)(l.Amount / dayPay) : 0;
                    return new MonthExpenseView(l.Id, T(l.Id), MoneyLabel(l.Amount), days, hasPay ? DaysLabel(days) : string.Empty);
                })
                .ToList();

            decimal total = player.LastExpenses.Sum(l => l.Amount);
            var tickets = new List<int>();
            int borrowed = 0;
            if (hasPay)
            {
                // Se reparten los días en orden: cada gasto se lleva los boletos hasta su total acumulado.
                double spent = 0;
                for (int i = 0; i < expenses.Count; i++)
                {
                    spent += expenses[i].Days;
                    int until = (int)Math.Round(spent, MidpointRounding.AwayFromZero);
                    while (tickets.Count < until)
                    {
                        tickets.Add(i);
                    }
                }

                borrowed = Math.Max(0, tickets.Count - MonthView.WorkDaysPerMonth);
                while (tickets.Count < MonthView.WorkDaysPerMonth)
                {
                    tickets.Add(MonthView.FreeTicket);
                }
            }

            string headline = expenses.Count == 0
                ? T("ui.mes.sin_gastos")
                : hasPay
                    ? Text("ui.mes.dias", tickets.Count(t => t != MonthView.FreeTicket))
                    : Text("ui.mes.sin_sueldo", MoneyLabel(total));
            string payLabel = hasPay ? Text("ui.mes.sueldo", MoneyLabel(net), MoneyLabel(dayPay)) : string.Empty;
            int free = tickets.Count(t => t == MonthView.FreeTicket);
            string footnote = !hasPay || expenses.Count == 0
                ? string.Empty
                : borrowed > 0 ? Text("ui.mes.prestados", borrowed) : Text("ui.mes.libres", free);
            return new MonthView(headline, payLabel, hasPay, expenses, tickets, borrowed, footnote);
        }

        /// <summary>Días de trabajo en palabras: "menos de un día", "1 día", "3 días".</summary>
        private string DaysLabel(double days)
        {
            int rounded = (int)Math.Round(days, MidpointRounding.AwayFromZero);
            return days < 0.5 ? T("ui.mes.menos_de_un_dia") : rounded == 1 ? T("ui.mes.un_dia") : Text("ui.mes.n_dias", rounded);
        }

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
            return phase == LifePhase.Living ? Session.Advance(maxWeeks) : 0;
        }

        private string T(string key) => Session.Text(key, Language);

        private JournalLine Line(JournalEntry entry) => new JournalLine(AgeLabel(Session.World.Player, entry.Date), T(entry.TextKey));

        private EventView EventOf(PendingEvent pending)
        {
            EventDefinition definition = Session.Content.GetEvent(pending.EventId);
            var options = pending.AvailableOptions.Select(i => T(definition.Options[i].TextKey)).ToList();
            int? previous = Session.TimeMachine.PreviousChoice(pending);
            int position = previous.HasValue ? IndexOf(pending.AvailableOptions, previous.Value) : -1;
            string textKey = definition.TextKeyFor(Session.World.TimesHappened(pending.EventId, pending.CharacterId));

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

            return new EventView(pending.EventId, T(textKey), options, position >= 0 ? position : (int?)null, definition.KeyDay, remembered, watching, noticedBy);
        }

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
        /// Quiénes están en la casa: el jugador, su pareja, sus hijos menores de edad
        /// y sus padres mientras sea menor de edad o, sin pareja, menor de
        /// <see cref="LeavesHomeAge"/> años. Es una aproximación para dibujar
        /// hasta que el núcleo tenga un sistema de vivienda.
        /// </summary>
        private HomeView Home(Character player)
        {
            World world = Session.World;
            int age = player.AgeInYears(world.Date);
            var members = new List<HomeMemberView> { Member(player, string.Empty, isPlayer: true) };

            if (age < 18 || (!player.PartnerId.HasValue && age < LeavesHomeAge))
            {
                AddIfAlive(members, player.MotherId, T("ui.relacion.madre"));
                AddIfAlive(members, player.FatherId, T("ui.relacion.padre"));
            }

            AddIfAlive(members, player.PartnerId, T("ui.relacion.pareja"));
            foreach (Character child in world.ChildrenOf(player.Id).Where(c => c.IsAlive && c.AgeInYears(world.Date) < 18))
            {
                members.Add(Member(child, ChildRelation(child), isPlayer: false));
            }

            return new HomeView(player.Residence?.CommuneId ?? string.Empty, player.HouseholdLevel, StageOf(age), members, SceneObjects(player));
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
            if (player.Degrees.Count == 0)
            {
                return new WorkplaceView(null, null, 1, freelance, figure, 0, companyText, sizeId);
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
            CareerPositionDefinition current = Session.Content.GetCareerPosition(position.PositionId);
            CareerDefinition jobCareer = Session.Content.GetCareer(current.CareerId);
            string caption = Text("ui.trabajo.cargo", T(position.PositionId), T(position.AreaId), player.Job.Performance);
            if (companyText != null)
            {
                caption = companyText + " · " + caption;
            }

            return new WorkplaceView(jobCareer.Id, jobCareer.WorkplaceKind, best.SalaryRelative, freelance, figure, RankOf(jobCareer, current), caption, sizeId);
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
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
        /// Al avanzar, el tiempo corre hasta que pasa algo, pero nunca más de estos
        /// años seguidos, para que el jugador vea pasar su vida.
        /// </summary>
        public const int MaxYearsPerStep = 5;

        /// <summary>Claves de texto que usa la pantalla; deben existir en el idioma base.</summary>
        public static readonly IReadOnlyList<string> TextKeys = new[]
        {
            "ui.edad.semanas", "ui.edad.un_ano", "ui.edad.anos", "ui.fecha", "ui.dinero", "ui.separador_miles",
            "ui.relacion.madre", "ui.relacion.padre", "ui.relacion.pareja", "ui.relacion.hija", "ui.relacion.hijo",
            "ui.boton.seguir_viviendo", "ui.boton.seguir_con", "ui.boton.nueva_vida", "ui.salud", "ui.felicidad", "ui.inteligencia",
            "ui.dinero_titulo", "ui.diario", "ui.familia", "ui.generacion", "ui.partida", "ui.legado", "ui.fin", "ui.fallecido",
            "ui.vista.casa", "ui.vista.barrio", "ui.lugar.casa", "ui.lugar.colegio", "ui.lugar.estudios", "ui.lugar.trabajo",
        };

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
                PlaceOf(player));
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
            return new EventView(pending.EventId, T(definition.ResolvedTextKey), options);
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

            return new HomeView(player.Residence?.CommuneId ?? string.Empty, player.HouseholdLevel, StageOf(age), members);
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
            return new HomeMemberView(character.Id, FullName(character), relation, age, StageOf(age), character.Sex == Sex.Female, isPlayer);
        }

        private string ChildRelation(Character child) => T(child.Sex == Sex.Female ? "ui.relacion.hija" : "ui.relacion.hijo");

        private PersonView Person(Character character, string relation)
        {
            GameDate at = character.DeathDate ?? Session.World.Date;
            return new PersonView(character.Id, FullName(character), relation, character.AgeInYears(at), character.IsAlive);
        }

        private static string FullName(Character character) => $"{character.FirstName} {character.LastName}";
    }
}

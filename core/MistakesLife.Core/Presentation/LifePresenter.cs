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
        };

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
                Session.Heirs.Select(h => Person(h, ChildRelation(h))).ToList());
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

        private string ChildRelation(Character child) => T(child.Sex == Sex.Female ? "ui.relacion.hija" : "ui.relacion.hijo");

        private PersonView Person(Character character, string relation)
        {
            GameDate at = character.DeathDate ?? Session.World.Date;
            return new PersonView(character.Id, FullName(character), relation, character.AgeInYears(at), character.IsAlive);
        }

        private static string FullName(Character character) => $"{character.FirstName} {character.LastName}";
    }
}

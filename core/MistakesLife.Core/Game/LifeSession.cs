using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Life;
using MistakesLife.Core.Saving;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Game
{
    /// <summary>
    /// Una partida lista para jugar: arma el mundo y el ciclo semanal con todos
    /// los sistemas, en el orden correcto. La usan la consola, el simulador de
    /// vidas y, más adelante, Unity. Ninguno de ellos debe armar el ciclo a mano.
    /// </summary>
    public sealed class LifeSession
    {
        private readonly WeekCycle _cycle;
        private int _journalRead;

        public LifeSession(ContentDatabase content, ulong seed)
            : this(content, seed, new SeedRules(), new EventRules())
        {
        }

        public LifeSession(ContentDatabase content, ulong seed, SeedRules seedRules, EventRules eventRules)
            : this(content ?? throw new ArgumentNullException(nameof(content)), NewWorld(content, seed), seedRules, eventRules, 0)
        {
        }

        /// <summary>
        /// Arma la partida sobre un mundo que ya existe (por ejemplo, uno cargado
        /// desde un guardado). El ciclo semanal se arma igual que en una partida nueva.
        /// </summary>
        internal LifeSession(ContentDatabase content, World world, SeedRules seedRules, EventRules eventRules, int journalRead)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            World = world ?? throw new ArgumentNullException(nameof(world));
            _journalRead = journalRead;
            Events = new EventSystem(content, eventRules);
            _cycle = new WeekCycle()
                .Register(new EconomySystem(content))
                .Register(new PayrollSystem(content))
                .Register(new PensionSystem(content))
                .Register(new TaxSystem(content))
                .Register(new ExpenseSystem(content))
                .Register(new DebtSystem(content))
                .Register(new EducationSystem(content))
                .Register(new CareerSystem())
                .Register(new AgingSystem())
                .Register(new MortalitySystem(content))
                .Register(new RelativesFamilySystem(content))
                .Register(new SeedSystem(seedRules))
                .Register(Events)
                .Register(new BirthdaySystem());
        }

        private static World NewWorld(ContentDatabase content, ulong seed)
        {
            World world = WorldFactory.CreateNewLife(seed, content);
            EconomySystem.Initialize(world, content);
            return world;
        }

        /// <summary>
        /// Carga una partida guardada con <see cref="SaveToText"/>. El contenido
        /// (paquetes) debe ser el mismo con que se jugó, o una versión más nueva.
        /// </summary>
        /// <exception cref="SaveFormatException">Si el texto no es un guardado válido.</exception>
        public static LifeSession Load(ContentDatabase content, string text) =>
            SaveGame.FromText(content, text, new SeedRules(), new EventRules());

        /// <summary>Igual que <see cref="Load(ContentDatabase, string)"/>, con reglas de semillas y eventos propias.</summary>
        public static LifeSession Load(ContentDatabase content, string text, SeedRules seedRules, EventRules eventRules) =>
            SaveGame.FromText(content, text, seedRules, eventRules);

        /// <summary>Carga una partida guardada con <see cref="SaveToBytes"/>.</summary>
        /// <exception cref="SaveFormatException">Si los bytes no son un guardado válido.</exception>
        public static LifeSession LoadFromBytes(ContentDatabase content, byte[] data) =>
            SaveGame.FromBytes(content, data, new SeedRules(), new EventRules());

        /// <summary>
        /// Guarda la partida completa como texto (JSON). No escribe archivos: quien
        /// la usa (Unity, la consola) decide dónde dejarla.
        /// </summary>
        public string SaveToText() => SaveGame.ToText(this);

        /// <summary>Guarda la partida completa comprimida, más liviana para el celular.</summary>
        public byte[] SaveToBytes() => SaveGame.ToBytes(this);

        /// <summary>Cuántas líneas del diario ya se entregaron con <see cref="TakeNewJournalEntries"/>.</summary>
        internal int JournalRead => _journalRead;

        public ContentDatabase Content { get; }

        public World World { get; }

        public EventSystem Events { get; }

        /// <summary>El evento que espera la decisión del jugador, si lo hay.</summary>
        public PendingEvent? Pending => World.PendingEvents.FirstOrDefault(p => p.CharacterId == World.PlayerId);

        public int PlayerAgeInYears => World.Player.AgeInYears(World.Date);

        /// <summary>
        /// Avanza hasta <paramref name="maxWeeks"/> semanas; se detiene antes si
        /// llega un evento o un día clave. Devuelve las semanas que pasaron.
        /// </summary>
        public int Advance(int maxWeeks)
        {
            if (Pending != null)
            {
                throw new InvalidOperationException("Primero hay que responder el evento pendiente.");
            }

            return _cycle.AdvanceWeeks(World, maxWeeks);
        }

        /// <summary>
        /// Los hijos vivos con quienes se puede seguir jugando cuando el jugador
        /// muere (del mayor al menor). Vacío mientras el jugador vive.
        /// </summary>
        public IReadOnlyList<Character> Heirs =>
            World.Player.IsAlive
                ? Array.Empty<Character>()
                : World.ChildrenOf(World.PlayerId).Where(c => c.IsAlive).ToList();

        /// <summary>La partida terminó: el jugador murió y no tiene hijos vivos.</summary>
        public bool IsOver => !World.Player.IsAlive && Heirs.Count == 0;

        /// <summary>
        /// Legado: el jugador murió y se sigue jugando con uno de sus hijos, en el
        /// mismo mundo. Las semillas heredables pasan al hijo; las demás siguen vivas
        /// a través de sus testigos.
        /// </summary>
        public void ContinueWith(int heirId)
        {
            Character deceased = World.Player;
            if (deceased.IsAlive)
            {
                throw new InvalidOperationException("El jugador sigue vivo: todavía no hay legado.");
            }

            Character heir = Heirs.FirstOrDefault(c => c.Id == heirId)
                ?? throw new InvalidOperationException($"El personaje {heirId} no es un hijo vivo del jugador.");

            foreach (PendingEvent pending in World.PendingEvents.Where(p => p.CharacterId == deceased.Id).ToList())
            {
                World.RemovePendingEvent(pending);
            }

            World.Seeds.PassToHeir(deceased.Id, heir.Id);
            GivePriorLife(heir);
            World.SetPlayer(heir.Id);
            World.WriteJournal(heir.Id, deceased.Gender == Gender.Female ? "diario.muerte_madre" : "diario.muerte_padre");
            World.WriteJournal(heir.Id, "diario.legado");
        }

        /// <summary>Da por vivido lo que su país dice para su edad (campo vida_previa).</summary>
        private void GivePriorLife(Character heir)
        {
            if (heir.Residence == null)
            {
                return;
            }

            int age = heir.AgeInYears(World.Date);
            foreach (PriorLifeDefinition prior in Content.GetCountry(heir.Residence.CountryId).PriorLife.Where(p => p.Age <= age))
            {
                foreach (string flag in prior.Flags)
                {
                    heir.SetFlag(flag);
                }
            }
        }

        /// <summary>Responde el evento pendiente con una de sus opciones disponibles.</summary>
        public void Choose(int optionIndex)
        {
            PendingEvent pending = Pending ?? throw new InvalidOperationException("No hay un evento pendiente.");
            Events.Choose(World, pending, optionIndex);
        }

        /// <summary>Entradas nuevas del diario del jugador desde la última vez que se pidieron.</summary>
        public IReadOnlyList<JournalEntry> TakeNewJournalEntries()
        {
            var entries = World.Journal.Skip(_journalRead).Where(e => e.CharacterId == World.PlayerId).ToList();
            _journalRead = World.Journal.Count;
            return entries;
        }

        /// <summary>Texto para el jugador en el idioma dado.</summary>
        public string Text(string key, string language = ContentDatabase.BaseLanguage) => Content.Text(key, language);
    }
}

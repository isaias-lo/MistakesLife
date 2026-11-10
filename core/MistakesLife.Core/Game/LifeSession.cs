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
            : this(content, seed, null, seedRules, eventRules)
        {
        }

        /// <summary>
        /// Partida nueva que nace en un país elegido (por ejemplo "pais.argentina").
        /// Con null, el país sale al azar según los paquetes.
        /// </summary>
        public LifeSession(ContentDatabase content, ulong seed, string? countryId)
            : this(content, seed, countryId, new SeedRules(), new EventRules())
        {
        }

        public LifeSession(ContentDatabase content, ulong seed, string? countryId, SeedRules seedRules, EventRules eventRules)
            : this(content ?? throw new ArgumentNullException(nameof(content)), NewWorld(content, seed, countryId), seedRules, eventRules, 0)
        {
            TimeMachine.SetCharges(TimeMachine.StartingCharges);
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
            TimeMachine = new TimeMachine(0);
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
                .Register(new CompanySystem(content))
                .Register(new JobSearchSystem(content))
                .Register(new AgingSystem())
                .Register(new MortalitySystem(content))
                .Register(new RelativesFamilySystem(content))
                .Register(new RelationshipSystem())
                .Register(new SeedSystem(seedRules))
                .Register(Events)
                .Register(new BirthdaySystem());
        }

        private static World NewWorld(ContentDatabase content, ulong seed, string? countryId)
        {
            World world = WorldFactory.CreateNewLife(seed, content, countryId);
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

        /// <summary>
        /// El mundo de la partida. Cambia por otro al volver al pasado
        /// (<see cref="GoBack(int)"/>): no conviene guardarlo aparte, siempre
        /// pedirlo aquí.
        /// </summary>
        public World World { get; private set; }

        /// <summary>Volver al pasado: fotos del mundo, cargas y etapas ya vividas de nuevo.</summary>
        public TimeMachine TimeMachine { get; }

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

            // Con la máquina del tiempo encendida, el tiempo se para un momento
            // al empezar cada etapa de la vida, para sacar su foto, y sigue.
            // La primera foto (la del nacimiento, o la de una partida vieja) se saca
            // al empezar a avanzar.
            TakeSnapshots();
            int total = 0;
            while (total < maxWeeks && World.Player.IsAlive)
            {
                int step = maxWeeks - total;
                int? toNextStage = TimeMachine.Enabled ? LifeStages.WeeksToNextStage(World.Player.AgeInWeeks(World.Date)) : null;
                if (toNextStage.HasValue && toNextStage.Value < step)
                {
                    step = toNextStage.Value;
                }

                total += _cycle.AdvanceWeeks(World, step, out bool stopped);
                TakeSnapshots();
                if (stopped)
                {
                    break;
                }
            }

            return total;
        }

        /// <summary>
        /// Vuelve al pasado: la partida sigue desde esa foto del mundo, como si lo
        /// que vino después no hubiera pasado. Gasta una carga y la etapa de la
        /// foto ya no se puede volver a vivir. El jugador conserva una intuición
        /// en su diario y, si la foto es de antes de una decisión, la opción que
        /// eligió la otra vez queda en <see cref="TimeMachine.PreviousChoice"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si no quedan cargas, la etapa ya se volvió a vivir o la foto no existe.</exception>
        public void GoBack(int snapshotId)
        {
            WorldSnapshot snapshot = TimeMachine.Snapshots.FirstOrDefault(s => s.Id == snapshotId)
                ?? throw new InvalidOperationException($"No hay una foto del mundo número {snapshotId}.");

            // Lo que eligió esa vez, antes de que ese futuro se borre.
            int? previous = null;
            if (snapshot.Kind == SnapshotKind.BeforeDecision)
            {
                previous = World.EventHistory
                    .FirstOrDefault(r => r.CharacterId == snapshot.PlayerId && r.EventId == snapshot.EventId && r.Date >= snapshot.Date)
                    ?.ChosenOption;
            }

            TimeMachine.Spend(snapshot);
            World = SaveGame.ReadSnapshot(Content, snapshot.Data, out int journalRead);
            _journalRead = journalRead;
            PendingEvent? again = Pending;
            if (previous.HasValue && again != null && again.EventId == snapshot.EventId)
            {
                TimeMachine.RememberChoice(again.EventId, again.CharacterId, again.Date, previous.Value);
            }

            World.WriteJournal(World.PlayerId, previous.HasValue ? JournalIntuitionDecision : JournalIntuition);
        }

        /// <summary>Diario al volver al pasado.</summary>
        public const string JournalIntuition = "diario.intuicion";

        /// <summary>Diario al volver a una decisión que ya se había tomado.</summary>
        public const string JournalIntuitionDecision = "diario.intuicion_decision";

        /// <summary>
        /// Saca las fotos que faltan: al empezar la etapa de la vida en que va el
        /// jugador y antes de una decisión de día clave que espera respuesta.
        /// </summary>
        private void TakeSnapshots()
        {
            Character player = World.Player;
            if (!TimeMachine.Enabled || !player.IsAlive)
            {
                return;
            }

            int age = player.AgeInYears(World.Date);
            string stage = LifeStages.StageAt(age);
            if (!TimeMachine.HasStageSnapshot(player.Id, stage))
            {
                TimeMachine.Add(NewSnapshot(SnapshotKind.StageStart, age, stage, null));
            }

            PendingEvent? pending = Pending;
            if (pending != null && Content.GetEvent(pending.EventId).KeyDay && !TimeMachine.HasDecisionSnapshot(pending, World.Date))
            {
                TimeMachine.Add(NewSnapshot(SnapshotKind.BeforeDecision, age, stage, pending.EventId));
            }
        }

        private WorldSnapshot NewSnapshot(SnapshotKind kind, int age, string stage, string? eventId) =>
            new WorldSnapshot(TimeMachine.NextSnapshotId, kind, World.Date, World.PlayerId, age, stage, eventId, SaveGame.SnapshotBytes(World, _journalRead));

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
            TimeMachine.StartNewGeneration();
            TakeSnapshots();
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

                if (prior.Housing != null && heir.Lease == null)
                {
                    Economy.HousingRules.AssumeLease(World, Content, heir, prior.Housing);
                }
            }
        }

        /// <summary>Responde el evento pendiente con una de sus opciones disponibles.</summary>
        public void Choose(int optionIndex)
        {
            PendingEvent pending = Pending ?? throw new InvalidOperationException("No hay un evento pendiente.");
            Events.Choose(World, pending, optionIndex);
            TakeSnapshots();
        }

        /// <summary>Entradas nuevas del diario del jugador desde la última vez que se pidieron.</summary>
        public IReadOnlyList<JournalEntry> TakeNewJournalEntries()
        {
            var entries = World.Journal.Skip(_journalRead).Where(e => e.CharacterId == World.PlayerId).ToList();
            _journalRead = World.Journal.Count;
            return entries;
        }

        /// <summary>
        /// Texto para el jugador en el idioma dado, con la forma de decirlo del país
        /// donde vive (por ejemplo "es" en Chile usa "es-CL").
        /// </summary>
        public string Text(string key, string language = ContentDatabase.BaseLanguage) =>
            Content.Text(key, Content.LanguageFor(World.Player.Residence?.CountryId, language));
    }
}

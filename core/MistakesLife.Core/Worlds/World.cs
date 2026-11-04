using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Worlds
{
    /// <summary>
    /// Todo el estado de una partida: fecha, personajes, diario y azar.
    /// La simulación solo cambia el mundo a través de los sistemas del ciclo semanal.
    /// </summary>
    public sealed class World
    {
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();
        private readonly List<JournalEntry> _journal = new List<JournalEntry>();
        private readonly List<PendingEvent> _pendingEvents = new List<PendingEvent>();
        private readonly List<EventRecord> _eventHistory = new List<EventRecord>();
        private readonly List<int> _formerPlayers = new List<int>();
        private readonly Dictionary<string, Scripting.Value> _lawChanges = new Dictionary<string, Scripting.Value>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, Economy.CountryEconomy> _economies = new SortedDictionary<string, Economy.CountryEconomy>(StringComparer.Ordinal);
        private int _nextCharacterId = 1;

        public World(ulong seed)
            : this(seed, GameDate.Start)
        {
        }

        /// <summary>Crea un mundo que empieza en una fecha dada (para que haya pasado: padres, abuelos).</summary>
        public World(ulong seed, GameDate startDate)
            : this(seed, startDate, seed)
        {
        }

        /// <summary>Crea un mundo con el azar en un estado dado (al cargar una partida).</summary>
        internal World(ulong seed, GameDate date, ulong randomState)
        {
            Seed = seed;
            Random = GameRandom.FromState(randomState);
            Date = date;
        }

        /// <summary>Semilla de azar de la partida. Con ella se repite la vida exacta.</summary>
        public ulong Seed { get; }

        /// <summary>Generador de azar principal de la partida.</summary>
        public GameRandom Random { get; }

        public GameDate Date { get; private set; }

        /// <summary>Id del personaje que controla el jugador.</summary>
        public int PlayerId { get; private set; }

        public Character Player => GetCharacter(PlayerId);

        public IReadOnlyCollection<Character> Characters => _characters.Values;

        public IReadOnlyList<JournalEntry> Journal => _journal;

        /// <summary>Todas las semillas (consecuencias diferidas) del mundo.</summary>
        public SeedBook Seeds { get; } = new SeedBook();

        /// <summary>Eventos que esperan que el jugador elija una opción.</summary>
        public IReadOnlyList<PendingEvent> PendingEvents => _pendingEvents;

        /// <summary>Eventos ya vividos.</summary>
        public IReadOnlyList<EventRecord> EventHistory => _eventHistory;

        internal void AddPendingEvent(PendingEvent pending) => _pendingEvents.Add(pending);

        internal void RemovePendingEvent(PendingEvent pending) => _pendingEvents.Remove(pending);

        internal void RecordEvent(EventRecord record) => _eventHistory.Add(record);

        /// <summary>El próximo id de personaje (para guardar la partida).</summary>
        internal int NextCharacterIdValue => _nextCharacterId;

        /// <summary>Todos los cambios de leyes, con clave "país|ley" (para guardar la partida).</summary>
        internal IReadOnlyDictionary<string, Scripting.Value> LawChanges => _lawChanges;

        /// <summary>Repone lo que no se puede reconstruir paso a paso (al cargar una partida).</summary>
        internal void RestoreState(int playerId, int nextCharacterId, IEnumerable<int> formerPlayers)
        {
            PlayerId = playerId;
            _nextCharacterId = nextCharacterId;
            _formerPlayers.Clear();
            _formerPlayers.AddRange(formerPlayers);
        }

        /// <summary>Repone un cambio de ley con su clave "país|ley" (al cargar una partida).</summary>
        internal void RestoreLawChange(string key, Scripting.Value value) => _lawChanges[key] = value;

        /// <summary>Repone una línea del diario tal como se guardó (al cargar una partida).</summary>
        internal void RestoreJournal(JournalEntry entry) => _journal.Add(entry);

        /// <summary>Entrega un id nuevo, que nunca se repite en este mundo.</summary>
        public int NextCharacterId() => _nextCharacterId++;

        public void AddCharacter(Character character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (_characters.ContainsKey(character.Id))
            {
                throw new InvalidOperationException($"Ya existe un personaje con id {character.Id}.");
            }

            _characters.Add(character.Id, character);
        }

        public Character GetCharacter(int id)
        {
            if (!_characters.TryGetValue(id, out Character? character))
            {
                throw new KeyNotFoundException($"No existe el personaje {id}.");
            }

            return character;
        }

        public bool TryGetCharacter(int id, [MaybeNullWhen(false)] out Character character)
        {
            return _characters.TryGetValue(id, out character);
        }

        /// <summary>Hijos de un personaje (vivos o no), del mayor al menor.</summary>
        public IReadOnlyList<Character> ChildrenOf(int parentId)
        {
            var children = new List<Character>();
            foreach (Character character in _characters.Values)
            {
                if (character.IsChildOf(parentId))
                {
                    children.Add(character);
                }
            }

            children.Sort((a, b) => a.BirthDate.TotalWeeks != b.BirthDate.TotalWeeks ? a.BirthDate.TotalWeeks.CompareTo(b.BirthDate.TotalWeeks) : a.Id.CompareTo(b.Id));
            return children;
        }

        public void SetPlayer(int characterId)
        {
            GetCharacter(characterId);
            if (PlayerId != 0 && PlayerId != characterId)
            {
                _formerPlayers.Add(PlayerId);
            }

            PlayerId = characterId;
        }

        /// <summary>
        /// Los personajes que el jugador controló antes (el linaje), del primero
        /// al último. Siguen en el mundo: lo que hicieron queda en la historia.
        /// </summary>
        public IReadOnlyList<int> FormerPlayers => _formerPlayers;

        /// <summary>
        /// Cambia una ley de un país durante la partida (por ejemplo, un evento
        /// histórico que la aprueba). Desde ahora vale para todos en ese país.
        /// </summary>
        public void ChangeLaw(string countryId, string lawId, Scripting.Value value) => _lawChanges[countryId + "|" + lawId] = value;

        /// <summary>El valor de una ley si cambió durante la partida.</summary>
        public bool TryGetChangedLaw(string countryId, string lawId, out Scripting.Value value) =>
            _lawChanges.TryGetValue(countryId + "|" + lawId, out value);

        /// <summary>La economía de un país durante la partida (precios e inflación), o null si el país no tiene datos de economía.</summary>
        public Economy.CountryEconomy? EconomyOf(string countryId) =>
            _economies.TryGetValue(countryId, out Economy.CountryEconomy? economy) ? economy : null;

        /// <summary>Las economías de todos los países, ordenadas por id.</summary>
        public IEnumerable<Economy.CountryEconomy> Economies => _economies.Values;

        internal void AddEconomy(Economy.CountryEconomy economy) => _economies.Add(economy.CountryId, economy);

        /// <summary>Generación del jugador actual: 1 para el primero, 2 para su hijo, etc.</summary>
        public int Generation => _formerPlayers.Count + 1;

        public void WriteJournal(int characterId, string textKey)
        {
            _journal.Add(new JournalEntry(Date, characterId, textKey));
        }

        /// <summary>Avanza la fecha una semana. Solo lo llama el ciclo semanal.</summary>
        internal void AdvanceWeek()
        {
            Date = Date.AddWeeks(1);
        }
    }
}

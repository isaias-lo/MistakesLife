using System;
using System.Collections.Generic;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Randomness;
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
        private int _nextCharacterId = 1;

        public World(ulong seed)
        {
            Seed = seed;
            Random = new GameRandom(seed);
            Date = GameDate.Start;
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

        public void SetPlayer(int characterId)
        {
            GetCharacter(characterId);
            PlayerId = characterId;
        }

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

using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Seeds
{
    /// <summary>
    /// Todas las semillas del mundo, de todos los personajes y generaciones.
    /// Se recorren siempre en orden de id, para que el azar sea reproducible.
    /// </summary>
    public sealed class SeedBook
    {
        private readonly SortedDictionary<int, Seed> _seeds = new SortedDictionary<int, Seed>();
        private readonly List<GameDate> _awakenings = new List<GameDate>();
        private int _nextId = 1;

        public IEnumerable<Seed> All => _seeds.Values;

        public IEnumerable<Seed> Dormant => _seeds.Values.Where(s => s.State == SeedState.Dormant);

        /// <summary>Semillas que despertaron y esperan su evento.</summary>
        public IEnumerable<Seed> Awake => _seeds.Values.Where(s => s.State == SeedState.Awake);

        /// <summary>Fechas en que despertó alguna semilla (para el límite por mes).</summary>
        public IReadOnlyList<GameDate> Awakenings => _awakenings;

        /// <summary>Planta una semilla nueva.</summary>
        public Seed Plant(SeedDefinition definition, int ownerId, GameDate date, int ownerAge, IEnumerable<int>? witnesses = null, int? parentSeedId = null)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var seed = new Seed(_nextId++, definition, ownerId, date, ownerAge, parentSeedId);
            if (witnesses != null)
            {
                foreach (int witness in witnesses)
                {
                    seed.AddWitness(witness);
                }
            }

            _seeds.Add(seed.Id, seed);
            return seed;
        }

        /// <summary>El próximo id de semilla (para guardar la partida).</summary>
        internal int NextId => _nextId;

        /// <summary>Repone una semilla guardada, con su id original (al cargar una partida).</summary>
        internal Seed Restore(int id, SeedDefinition definition, int ownerId, GameDate plantedDate, int ownerAgeAtPlanting, int? parentSeedId)
        {
            var seed = new Seed(id, definition, ownerId, plantedDate, ownerAgeAtPlanting, parentSeedId);
            _seeds.Add(seed.Id, seed);
            return seed;
        }

        /// <summary>Repone el contador de ids y las fechas de despertar (al cargar una partida).</summary>
        internal void RestoreCounters(int nextId, IEnumerable<GameDate> awakenings)
        {
            _nextId = nextId;
            _awakenings.Clear();
            _awakenings.AddRange(awakenings);
        }

        public Seed Get(int id)
        {
            if (!_seeds.TryGetValue(id, out Seed? seed))
            {
                throw new KeyNotFoundException($"No existe la semilla {id}.");
            }

            return seed;
        }

        /// <summary>Semillas abiertas (dormidas o despiertas) de un personaje.</summary>
        public IEnumerable<Seed> OpenFor(int ownerId) => _seeds.Values.Where(s => s.OwnerId == ownerId && s.IsOpen);

        /// <summary>Semillas abiertas de un tipo, por ejemplo todas las "semilla.copio_paes".</summary>
        public IEnumerable<Seed> OpenOfType(string typeId) => _seeds.Values.Where(s => s.TypeId == typeId && s.IsOpen);

        /// <summary>Cuántas semillas despertaron en el rango [desde, hasta].</summary>
        public int AwakeningsBetween(GameDate from, GameDate to) => _awakenings.Count(d => d >= from && d <= to);

        public void Awaken(Seed seed, GameDate date)
        {
            seed.Awaken(date);
            _awakenings.Add(date);
        }

        public void Expire(Seed seed, GameDate date) => seed.Expire(date);

        /// <summary>
        /// El evento de la semilla se jugó: se cierra. Si se indica, deja una
        /// semilla nueva (por ejemplo, la lealtad hacia quien te contrató).
        /// </summary>
        public Seed? Resolve(Seed seed, GameDate date, SeedDefinition? followUp = null, int ownerAge = 0, IEnumerable<int>? witnesses = null)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            seed.Resolve(date);
            return followUp == null ? null : Plant(followUp, seed.OwnerId, date, ownerAge, witnesses, seed.Id);
        }

        /// <summary>
        /// Legado: al morir un personaje, sus semillas heredables pasan a su hijo.
        /// Las demás siguen vivas en el mundo a través de sus testigos.
        /// Devuelve cuántas pasaron.
        /// </summary>
        public int PassToHeir(int deceasedId, int heirId)
        {
            int count = 0;
            foreach (Seed seed in OpenFor(deceasedId).Where(s => s.Heritable).ToList())
            {
                seed.TransferTo(heirId);
                count++;
            }

            return count;
        }
    }
}

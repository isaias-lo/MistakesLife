using System;

namespace MistakesLife.Core.Randomness
{
    /// <summary>
    /// Generador de azar reproducible (algoritmo SplitMix64).
    /// Con la misma semilla entrega siempre la misma secuencia, en cualquier
    /// computador o celular. Toda la simulación debe usar este generador:
    /// nunca System.Random ni la hora del sistema.
    /// Su estado es un solo número, así que se guarda fácil con la partida.
    /// </summary>
    public sealed class GameRandom
    {
        private ulong _state;

        public GameRandom(ulong seed)
        {
            _state = seed;
        }

        /// <summary>Estado interno actual, para guardarlo con la partida.</summary>
        public ulong State => _state;

        /// <summary>Restaura un generador guardado.</summary>
        public static GameRandom FromState(ulong state) => new GameRandom(state);

        public ulong NextULong()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Número decimal en [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>Entero en [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Debe ser mayor que 0.");
            }

            // Rechazo para evitar sesgo.
            ulong bound = (ulong)maxExclusive;
            ulong limit = ulong.MaxValue - (ulong.MaxValue % bound);
            ulong value;
            do
            {
                value = NextULong();
            }
            while (value >= limit);
            return (int)(value % bound);
        }

        /// <summary>Entero en [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Debe ser mayor que el mínimo.");
            }

            return minInclusive + NextInt(maxExclusive - minInclusive);
        }

        /// <summary>
        /// Elige una posición al azar según pesos (un peso 3 sale el triple que un peso 1).
        /// </summary>
        public int PickWeightedIndex(System.Collections.Generic.IReadOnlyList<int> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                throw new ArgumentException("Se necesita al menos un peso.", nameof(weights));
            }

            int total = 0;
            foreach (int weight in weights)
            {
                if (weight <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), "Los pesos deben ser mayores que 0.");
                }

                total += weight;
            }

            int roll = NextInt(total);
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    return i;
                }
            }

            return weights.Count - 1;
        }

        /// <summary>Elige un elemento de una lista al azar.</summary>
        public T Pick<T>(System.Collections.Generic.IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("La lista está vacía.", nameof(items));
            }

            return items[NextInt(items.Count)];
        }

        /// <summary>Devuelve true con la probabilidad indicada (0 a 1).</summary>
        public bool Chance(double probability) => NextDouble() < probability;

        /// <summary>
        /// Crea un generador independiente para un propósito con nombre
        /// (por ejemplo "personalidad" o "sistema.salud"). Usar flujos separados
        /// hace que agregar un sistema nuevo no cambie el azar de los demás.
        /// No avanza este generador.
        /// </summary>
        public GameRandom Fork(string purpose)
        {
            if (purpose == null)
            {
                throw new ArgumentNullException(nameof(purpose));
            }

            return new GameRandom(Mix(_state ^ StableHash(purpose)));
        }

        /// <summary>Hash FNV-1a de 64 bits: estable entre plataformas y versiones.</summary>
        public static ulong StableHash(string text)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }

        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}

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

using System;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Estadísticas visibles básicas de un personaje, de 0 a 100.
    /// Se ampliarán a medida que lleguen los sistemas (salud, estudios, dinero…).
    /// </summary>
    public sealed class Stats
    {
        public const int Min = 0;
        public const int Max = 100;

        private int _health;
        private int _happiness;
        private int _smarts;
        private int _energy;
        private int _looks;

        /// <summary>Con qué energía parte alguien que recién aparece: descansado.</summary>
        public const int StartingEnergy = 80;

        /// <summary>Apariencia de alguien que todavía no la tiene calculada (a media escala).</summary>
        public const int UnknownLooks = 50;

        public Stats(int health, int happiness, int smarts)
            : this(health, happiness, smarts, StartingEnergy, UnknownLooks)
        {
        }

        public Stats(int health, int happiness, int smarts, int energy, int looks)
        {
            Health = health;
            Happiness = happiness;
            Smarts = smarts;
            Energy = energy;
            Looks = looks;
        }

        public int Health
        {
            get => _health;
            set => _health = Clamp(value);
        }

        public int Happiness
        {
            get => _happiness;
            set => _happiness = Clamp(value);
        }

        public int Smarts
        {
            get => _smarts;
            set => _smarts = Clamp(value);
        }

        /// <summary>
        /// Cuánta energía le queda (la batería del celular la refleja). Baja con un horario
        /// lleno, la pega, los estudios, los hijos chicos y la enfermedad; sube con descanso
        /// (ver <see cref="Life.EnergyRules"/>).
        /// </summary>
        public int Energy
        {
            get => _energy;
            set => _energy = Clamp(value);
        }

        /// <summary>
        /// Cómo se ve: parte de lo que heredó de su mamá y su papá y cambia con sus hábitos,
        /// su salud y su edad (ver <see cref="Life.LooksRules"/>).
        /// </summary>
        public int Looks
        {
            get => _looks;
            set => _looks = Clamp(value);
        }

        private static int Clamp(int value) => Math.Max(Min, Math.Min(Max, value));
    }
}

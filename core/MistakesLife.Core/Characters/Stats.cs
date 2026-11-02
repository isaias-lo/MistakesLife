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

        public Stats(int health, int happiness, int smarts)
        {
            Health = health;
            Happiness = happiness;
            Smarts = smarts;
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

        private static int Clamp(int value) => Math.Max(Min, Math.Min(Max, value));
    }
}

using System;
using MistakesLife.Core.Randomness;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Los cinco grandes rasgos de personalidad, de 0 a 100.
    /// El jugador nunca ve estos números: los descubre por reacciones,
    /// el diario y lo que dicen otros.
    /// </summary>
    public sealed class Personality
    {
        public const int Min = 0;
        public const int Max = 100;

        public Personality(int openness, int conscientiousness, int extraversion, int agreeableness, int neuroticism)
        {
            Openness = Clamp(openness);
            Conscientiousness = Clamp(conscientiousness);
            Extraversion = Clamp(extraversion);
            Agreeableness = Clamp(agreeableness);
            Neuroticism = Clamp(neuroticism);
        }

        /// <summary>Apertura a la experiencia.</summary>
        public int Openness { get; }

        /// <summary>Responsabilidad.</summary>
        public int Conscientiousness { get; }

        /// <summary>Extraversión.</summary>
        public int Extraversion { get; }

        /// <summary>Amabilidad.</summary>
        public int Agreeableness { get; }

        /// <summary>Neuroticismo (inestabilidad emocional).</summary>
        public int Neuroticism { get; }

        /// <summary>
        /// Personalidad al azar, concentrada cerca de 50 (como en la población real).
        /// Más adelante se mezclará con la genética de los padres.
        /// </summary>
        public static Personality Random(GameRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return new Personality(Trait(random), Trait(random), Trait(random), Trait(random), Trait(random));
        }

        private static int Trait(GameRandom random)
        {
            // Promedio de tres tiradas: forma de campana sin matemática complicada.
            int sum = random.NextInt(0, 101) + random.NextInt(0, 101) + random.NextInt(0, 101);
            return sum / 3;
        }

        private static int Clamp(int value) => Math.Max(Min, Math.Min(Max, value));
    }
}

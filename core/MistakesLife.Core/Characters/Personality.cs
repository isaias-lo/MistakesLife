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
        /// Para un hijo, usa <see cref="Inherit"/>.
        /// </summary>
        public static Personality Random(GameRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return new Personality(Trait(random), Trait(random), Trait(random), Trait(random), Trait(random));
        }

        /// <summary>
        /// Personalidad de un hijo: cada rasgo es la mitad herencia (el promedio de
        /// los padres) y la mitad propia (al azar). Así se parece a sus padres pero
        /// no es una copia, como pasa en la vida real (la personalidad se hereda
        /// cerca de un 40 a 50 %). Si solo se conoce a un padre, se usa ese.
        /// </summary>
        public static Personality Inherit(Personality parent, Personality? otherParent, GameRandom random)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            Personality other = otherParent ?? parent;
            return new Personality(
                Mix(parent.Openness, other.Openness, random),
                Mix(parent.Conscientiousness, other.Conscientiousness, random),
                Mix(parent.Extraversion, other.Extraversion, random),
                Mix(parent.Agreeableness, other.Agreeableness, random),
                Mix(parent.Neuroticism, other.Neuroticism, random));
        }

        private static int Mix(int a, int b, GameRandom random) => (((a + b) / 2) + Trait(random)) / 2;

        private static int Trait(GameRandom random)
        {
            // Promedio de tres tiradas: forma de campana sin matemática complicada.
            int sum = random.NextInt(0, 101) + random.NextInt(0, 101) + random.NextInt(0, 101);
            return sum / 3;
        }

        private static int Clamp(int value) => Math.Max(Min, Math.Min(Max, value));
    }
}

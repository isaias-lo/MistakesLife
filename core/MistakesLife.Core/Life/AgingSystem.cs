using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de sistemas: la salud con los años. Cada edad tiene una "salud
    /// esperada" (85 hasta los 40 años, que baja de a poco hasta 40 a los 90).
    /// La salud de cada persona se acerca lentamente a ese valor: si un evento
    /// la bajó (un resfrío, un robo, el estrés), el cuerpo se recupera; si la
    /// persona envejece, la salud baja aunque no pase nada.
    ///
    /// Se mueve en promedio <see cref="DriftPointsPerYear"/> puntos al año: cada
    /// semana hay una pequeña probabilidad de moverse un punto. Los malos hábitos
    /// (eventos que restan salud una y otra vez) le ganan a esa recuperación.
    /// </summary>
    public sealed class AgingSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.envejecimiento";

        /// <summary>Puntos de salud al año que se acerca la salud a la esperada.</summary>
        public const double DriftPointsPerYear = 4;

        /// <summary>Edad y salud esperada, de menor a mayor edad; entre dos puntos, línea recta.</summary>
        private static readonly (int Age, int Health)[] ExpectedHealthByAge =
        {
            (0, 85),
            (40, 85),
            (60, 75),
            (75, 60),
            (90, 40),
        };

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            foreach (Character character in world.Characters.Where(c => c.IsAlive).OrderBy(c => c.Id))
            {
                int expected = ExpectedHealth(character.AgeInYears(context.Date));
                int health = character.Stats.Health;
                if (health == expected)
                {
                    continue;
                }

                if (context.RandomFor(SystemId + "." + character.Id).Chance(DriftPointsPerYear / GameDate.WeeksPerYear))
                {
                    character.Stats.Health += Math.Sign(expected - health);
                }
            }
        }

        /// <summary>La salud hacia la que tiende una persona de esa edad.</summary>
        public static int ExpectedHealth(int ageInYears)
        {
            for (int i = 1; i < ExpectedHealthByAge.Length; i++)
            {
                (int age, int health) = ExpectedHealthByAge[i];
                if (ageInYears <= age)
                {
                    (int previousAge, int previousHealth) = ExpectedHealthByAge[i - 1];
                    double share = (double)(ageInYears - previousAge) / (age - previousAge);
                    return (int)Math.Round(previousHealth + ((health - previousHealth) * share));
                }
            }

            return ExpectedHealthByAge[ExpectedHealthByAge.Length - 1].Health;
        }
    }
}

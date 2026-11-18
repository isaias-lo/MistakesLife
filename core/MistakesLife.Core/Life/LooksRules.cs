using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Las reglas de la apariencia (aprobadas por Isaias el 16-11-2026, ver docs/diseno-juego.md).
    /// Al nacer viene de tu mamá y tu papá, con un poco de azar de la partida. Después cambia
    /// con hábitos reales (lo que aporta cada actividad del horario: "aporta": { "apariencia": ... }),
    /// con tu salud y con la edad: sube en la juventud y baja de a poco desde los 40 o 50.
    /// La ropa y la peluquería se suman cuando exista Compras.
    /// </summary>
    public static class LooksRules
    {
        /// <summary>Cuánto puede alejarse un hijo del promedio de sus papás (más o menos).</summary>
        public const int InheritSpread = 12;

        /// <summary>Lo mínimo y lo máximo que se hereda.</summary>
        public const int MinInherited = 20;
        public const int MaxInherited = 90;

        /// <summary>Lo más que suman o restan los hábitos del horario.</summary>
        public const int MaxHabits = 10;

        /// <summary>Desde esta salud hacia arriba la salud ayuda a verse bien; bajo ella, se nota.</summary>
        public const int HealthyFrom = 60;

        /// <summary>Edad y cuánto suma o resta, de menor a mayor; entre dos puntos, línea recta.</summary>
        private static readonly (int Age, int Change)[] ByAge =
        {
            (0, 0),
            (12, 0),
            (18, 4),
            (35, 4),
            (45, 1),
            (55, -3),
            (65, -7),
            (80, -12),
        };

        /// <summary>
        /// La apariencia que hereda alguien: el promedio de lo heredado por su mamá y su papá
        /// (o de quien esté en el mundo) más o menos <see cref="InheritSpread"/>. Si no tiene
        /// papás en el mundo, sale del azar de la partida (dos tiradas promediadas: lo más común es lo del medio).
        /// </summary>
        public static int Inherit(World world, Character character, GameRandom random)
        {
            // Un hijo adoptado no hereda de quienes lo adoptaron: sus papás biológicos no están en el mundo.
            int? mother = character.Adopted ? null : ParentLooks(world, character.MotherId, random);
            int? father = character.Adopted ? null : ParentLooks(world, character.FatherId, random);
            int value;
            if (mother.HasValue || father.HasValue)
            {
                int sum = (mother ?? father!.Value) + (father ?? mother!.Value);
                value = (sum / 2) + random.NextInt(-InheritSpread, InheritSpread + 1);
            }
            else
            {
                value = (random.NextInt(MinInherited, MaxInherited + 1) + random.NextInt(MinInherited, MaxInherited + 1)) / 2;
            }

            return Math.Max(MinInherited, Math.Min(MaxInherited, value));
        }

        /// <summary>Lo heredado de un papá o una mamá (calculándolo si todavía no lo tenía).</summary>
        private static int? ParentLooks(World world, int? parentId, GameRandom random)
        {
            if (!parentId.HasValue || !world.TryGetCharacter(parentId.Value, out Character? parent))
            {
                return null;
            }

            if (!parent.InheritedLooks.HasValue)
            {
                parent.InheritedLooks = Inherit(world, parent, random.Fork("persona." + parent.Id));
                parent.Stats.Looks = parent.InheritedLooks.Value;
            }

            return parent.InheritedLooks.Value;
        }

        /// <summary>La apariencia hacia la que lleva tu vida: lo heredado, tus hábitos, tu salud y tu edad.</summary>
        public static int Target(World world, ContentDatabase content, Character character)
        {
            int target = character.InheritedLooks ?? Stats.UnknownLooks;
            Routine? routine = character.Routine;
            if (routine != null)
            {
                int habits = RoutineRules.Total(world, content, character, routine, "apariencia");
                target += Math.Max(-MaxHabits, Math.Min(MaxHabits, habits));
            }

            target += (character.Stats.Health - HealthyFrom) / 5;
            target += AgeChange(character.AgeInYears(world.Date));
            return Math.Max(Stats.Min, Math.Min(Stats.Max, target));
        }

        /// <summary>Lo que suma o resta la edad (sube en la juventud, baja de a poco desde los 45).</summary>
        public static int AgeChange(int ageInYears)
        {
            for (int i = 1; i < ByAge.Length; i++)
            {
                (int age, int change) = ByAge[i];
                (int previousAge, int previousChange) = ByAge[i - 1];
                if (ageInYears <= age)
                {
                    double t = (ageInYears - previousAge) / (double)(age - previousAge);
                    return (int)Math.Round(previousChange + ((change - previousChange) * t));
                }
            }

            return ByAge[ByAge.Length - 1].Change;
        }
    }
}

using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Las reglas de la energía (aprobadas por Isaias el 16-11-2026, ver docs/diseno-juego.md).
    /// Cada semana la energía se acerca a la que deja tu vida: baja con un horario lleno de
    /// pega, estudio o actividades, con los hijos chicos y cuando estás enfermo; sube con
    /// tardes libres, descanso y un poco de deporte. Con más edad cuesta más recuperarla.
    /// Lo que aporta cada actividad del horario está en su paquete ("aporta": { "energia": ... }).
    /// </summary>
    public static class EnergyRules
    {
        /// <summary>La energía de alguien descansado, sin pega, sin estudios fuera del colegio y sin hijos chicos.</summary>
        public const int Rested = 80;

        /// <summary>Cuánto suma cada bloque libre del horario (una tarde o una noche sin nada).</summary>
        public const int PerFreeBlock = 1;

        /// <summary>Lo que cansa tener una pega (cualquier trabajo con sueldo).</summary>
        public const int Work = 14;

        /// <summary>Lo que cansa estudiar una carrera (universidad, instituto o CFT).</summary>
        public const int Studies = 8;

        /// <summary>Lo que cansa el primer hijo de menos de <see cref="SmallChildAge"/> años en tu casa, y cada uno de los que siguen.</summary>
        public const int FirstSmallChild = 12;
        public const int OtherSmallChild = 5;

        /// <summary>Hasta esta edad (sin cumplirla) un hijo no deja dormir ni descansar.</summary>
        public const int SmallChildAge = 6;

        /// <summary>Bajo esta salud la enfermedad se come la energía: la mitad de lo que falta.</summary>
        public const int SickBelow = 50;

        /// <summary>Lo más baja y lo más alta que puede llevar la vida a la energía.</summary>
        public const int MinTarget = 5;
        public const int MaxTarget = 95;

        /// <summary>Puntos por semana que baja la energía cuando la vida cansa más de lo que descansa.</summary>
        public const int DrainPerWeek = 2;

        /// <summary>Bajo esta energía rindes menos en el estudio y en la pega.</summary>
        public const int TiredBelow = 30;

        /// <summary>La energía hacia la que lleva tu vida esta semana.</summary>
        public static int Target(World world, ContentDatabase content, Character character)
        {
            int target = Rested;
            Routine? routine = character.Routine;
            if (routine != null)
            {
                target += RoutineRules.Total(world, content, character, routine, "energia");
                foreach (string? slot in routine.Slots)
                {
                    if (slot == null)
                    {
                        target += PerFreeBlock;
                    }
                }
            }

            if (character.Job != null)
            {
                target -= Work;
            }

            if (character.Studies != null)
            {
                target -= Studies;
            }

            int small = CustodyRules.ChildrenAtHomeAged(world, character, world.Date, 0, SmallChildAge - 1);
            if (small > 0)
            {
                target -= FirstSmallChild + ((small - 1) * OtherSmallChild);
            }

            if (character.Stats.Health < SickBelow)
            {
                target -= (SickBelow - character.Stats.Health) / 2;
            }

            return Math.Max(MinTarget, Math.Min(MaxTarget, target));
        }

        /// <summary>
        /// Cuánto se recupera por semana a esa edad: 2 puntos hasta los 30, 1 hasta los 60 y,
        /// desde los 60, 1 punto cada dos semanas (<paramref name="weekOfYear"/> par).
        /// </summary>
        public static int RecoveryPerWeek(int ageInYears, int weekOfYear) =>
            ageInYears < 30 ? 2 : ageInYears < 60 ? 1 : weekOfYear % 2 == 0 ? 1 : 0;

        /// <summary>
        /// Lo que la falta de energía le quita a lo que rindes (en el estudio y la pega): la
        /// mitad de lo que te falta para <see cref="TiredBelow"/>, o nada si no estás cansado.
        /// </summary>
        public static int TiredPenalty(Character character) =>
            character.Stats.Energy < TiredBelow ? (TiredBelow - character.Stats.Energy) / 2 : 0;
    }
}

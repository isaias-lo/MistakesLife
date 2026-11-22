using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Life;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Los talleres del colegio (D5a3b): actuación, música o fútbol. Se inscribe uno por año
    /// (<c>jugador.inscribir_taller('musica')</c>) y se hace en dos tardes de su horario, con la
    /// actividad <c>actividad.taller.&lt;id&gt;</c> de los paquetes. Lo que se hace en el taller
    /// queda en una marca (<c>hizo_taller_musica</c>) para los caminos de más adelante.
    /// </summary>
    public static class WorkshopRules
    {
        /// <summary>El prefijo de la actividad del horario de cada taller.</summary>
        public const string ActivityPrefix = "actividad.taller.";

        /// <summary>Cuántas tardes a la semana se va al taller.</summary>
        public const int AfternoonsPerWeek = 2;

        /// <summary>Las tardes que se prueban primero: martes y jueves (lunes es 0).</summary>
        private static readonly int[] PreferredDays = { 1, 3, 0, 2, 4 };

        /// <summary>
        /// Se inscribe en el taller: queda como su taller del año, con su marca para después,
        /// y ocupa dos tardes de lunes a viernes que estén libres (vacías o de tiempo muerto).
        /// Si no le quedan tardes libres, el taller queda inscrito y lo pone en su horario él.
        /// </summary>
        public static void Enroll(World world, ContentDatabase content, Character character, string workshopId)
        {
            if (string.IsNullOrWhiteSpace(workshopId))
            {
                throw new ArgumentException("Falta el taller.", nameof(workshopId));
            }

            SchoolRecord record = character.SchoolRecord ??= new SchoolRecord();
            string activityId = ActivityPrefix + workshopId;
            if (content.Activity(activityId) == null)
            {
                throw new ArgumentException($"No existe la actividad {activityId} para el taller {workshopId}.", nameof(workshopId));
            }

            record.WorkshopId = workshopId;
            character.SetFlag("hizo_taller_" + workshopId);
            Routine? routine = character.Routine;
            if (routine == null)
            {
                return;
            }

            int placed = routine.Count(activityId);
            foreach (int day in PreferredDays)
            {
                if (placed >= AfternoonsPerWeek)
                {
                    break;
                }

                string? current = routine.Get(day, RoutineBlock.Afternoon);
                if (current == activityId)
                {
                    continue;
                }

                if (current == null || (content.Activity(current)?.IsFreeTime ?? false) || current.StartsWith(ActivityPrefix, StringComparison.Ordinal))
                {
                    routine.Set(day, RoutineBlock.Afternoon, activityId);
                    placed++;
                }
            }
        }
    }
}

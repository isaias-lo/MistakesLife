using System;
using System.Collections.Generic;

namespace MistakesLife.Core.Game
{
    /// <summary>
    /// Las etapas de la vida para volver al pasado: al empezar cada una se guarda
    /// una foto del mundo, y cada etapa se puede volver a vivir una sola vez, para
    /// que las decisiones sigan pesando. Los ids son también las claves de texto.
    /// No confundir con las etapas de la casa en 3D (solo para dibujar) ni con las
    /// etapas educativas de cada país.
    /// </summary>
    public static class LifeStages
    {
        public const string Infancy = "etapa_vida.infancia";
        public const string Childhood = "etapa_vida.ninez";
        public const string Adolescence = "etapa_vida.adolescencia";
        public const string Youth = "etapa_vida.juventud";
        public const string Adulthood = "etapa_vida.adultez";
        public const string Maturity = "etapa_vida.madurez";
        public const string OldAge = "etapa_vida.vejez";

        private static readonly string[] StageIds = { Infancy, Childhood, Adolescence, Youth, Adulthood, Maturity, OldAge };

        /// <summary>Edad (en años) a la que empieza cada etapa, en el mismo orden que <see cref="All"/>.</summary>
        private static readonly int[] StartAges = { 0, 6, 12, 18, 30, 45, 65 };

        /// <summary>Todas las etapas, de la primera a la última.</summary>
        public static IReadOnlyList<string> All => StageIds;

        /// <summary>La etapa de alguien con esa edad en años.</summary>
        public static string StageAt(int ageInYears)
        {
            for (int i = StartAges.Length - 1; i > 0; i--)
            {
                if (ageInYears >= StartAges[i])
                {
                    return StageIds[i];
                }
            }

            return StageIds[0];
        }

        /// <summary>Edad (en años) a la que empieza la etapa.</summary>
        public static int StartAge(string stageId)
        {
            int index = Array.IndexOf(StageIds, stageId);
            if (index < 0)
            {
                throw new ArgumentException($"No existe la etapa de vida '{stageId}'.", nameof(stageId));
            }

            return StartAges[index];
        }

        /// <summary>
        /// Semanas que faltan para que empiece la etapa siguiente, contando desde
        /// una edad en semanas; null en la última etapa.
        /// </summary>
        public static int? WeeksToNextStage(int ageInWeeks)
        {
            foreach (int start in StartAges)
            {
                int startWeek = start * Time.GameDate.WeeksPerYear;
                if (startWeek > ageInWeeks)
                {
                    return startWeek - ageInWeeks;
                }
            }

            return null;
        }
    }
}

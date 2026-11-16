using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Reglas de las notas del colegio: de qué depende una nota, cómo se saca el promedio y
    /// quién pasa de curso. La escala (de 1,0 a 7,0 en Chile), los ramos y las formas de
    /// pasar de curso vienen del paquete del país.
    /// </summary>
    public static class GradeRules
    {
        /// <summary>
        /// Una nota se calcula en una escala de 1 a 7 y después se lleva a la del país. Con
        /// inteligencia y constancia de la mitad, un horario sin estudio y sin preparar nada,
        /// la nota ronda el 5,0 (en Chile el promedio de enseñanza media ronda el 5,6 a 5,8).
        /// </summary>
        public const double BaseMark = 3.4;

        /// <summary>Lo que suma la inteligencia (0 a 100), como máximo.</summary>
        public const double SmartsWeight = 2.2;

        /// <summary>Lo que suma la constancia (responsabilidad, 0 a 100), como máximo.</summary>
        public const double ConscientiousnessWeight = 0.6;

        /// <summary>Lo que suma, como máximo, un horario con harto estudio (lo que deja en "mente").</summary>
        public const double StudyWeight = 0.8;

        /// <summary>Cuánta "mente" del horario es estudiar harto (de ahí no suma más).</summary>
        public const int FullStudy = 8;

        /// <summary>Lo que suma cada punto de preparación de una prueba anunciada (0 a 3).</summary>
        public const double PreparationWeight = 0.3;

        /// <summary>Preparación máxima de una prueba.</summary>
        public const int MaxPreparation = 3;

        /// <summary>Lo que resta andar muy triste (felicidad bajo <see cref="SadHappiness"/>).</summary>
        public const double SadPenalty = 0.4;

        /// <summary>Ver <see cref="SadPenalty"/>.</summary>
        public const int SadHappiness = 25;

        /// <summary>Cuántas pruebas anunciadas (que se juegan como momento) hay en un año.</summary>
        public const int AnnouncedTestsPerYear = 4;

        /// <summary>Semanas mínimas entre una prueba anunciada y la siguiente.</summary>
        public const int WeeksBetweenAnnouncedTests = 4;

        /// <summary>Los ramos que tiene hoy, según su etapa y su curso.</summary>
        public static IReadOnlyList<SubjectDefinition> Subjects(EducationStageDefinition stage, int level) =>
            stage.Subjects.Where(s => s.InLevel(level)).ToList();

        /// <summary>
        /// Una nota en la escala del país. Depende de la inteligencia, la constancia, cuánto
        /// estudio deja su horario, cuánto preparó esta prueba, el ánimo y algo de suerte.
        /// </summary>
        public static double Mark(World world, ContentDatabase content, Character character, GradeScaleDefinition scale, int preparation, GameRandom random)
        {
            double study = 0;
            if (character.Routine != null)
            {
                int mind = RoutineRules.Total(world, content, character, character.Routine, "mente");
                study = Math.Max(-0.5, Math.Min(1.0, mind / (double)FullStudy));
            }

            double luck = random.NextDouble() + random.NextDouble() + random.NextDouble() - 1.5;
            double mark = BaseMark
                + (SmartsWeight * character.Stats.Smarts / 100.0)
                + (ConscientiousnessWeight * character.Personality.Conscientiousness / 100.0)
                + (StudyWeight * study)
                + (PreparationWeight * Math.Max(0, Math.Min(MaxPreparation, preparation)))
                - (character.Stats.Happiness < SadHappiness ? SadPenalty : 0)
                + luck;
            mark = Math.Max(1.0, Math.Min(7.0, mark));
            return Round(scale.Min + ((mark - 1.0) / 6.0 * (scale.Max - scale.Min)), scale);
        }

        /// <summary>Redondea como en el país (en Chile, a un decimal: 4,45 es 4,5).</summary>
        public static double Round(double value, GradeScaleDefinition scale) =>
            Math.Round(value, scale.Decimals, MidpointRounding.AwayFromZero);

        /// <summary>La nota final de cada ramo con notas: el promedio de sus notas, redondeado.</summary>
        public static SortedDictionary<string, double> Finals(SchoolRecord record, GradeScaleDefinition scale)
        {
            var finals = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (var subject in record.Marks.Where(m => m.Value.Count > 0))
            {
                finals[subject.Key] = Round(subject.Value.Average(), scale);
            }

            return finals;
        }

        /// <summary>El promedio general: el promedio de las notas finales, redondeado; null si no hay.</summary>
        public static double? General(IReadOnlyDictionary<string, double> finals, GradeScaleDefinition scale) =>
            finals.Count == 0 ? (double?)null : Round(finals.Values.Average(), scale);

        /// <summary>Si con esas notas finales se pasa de curso (basta cumplir una de las formas de la etapa).</summary>
        public static bool Passes(EducationStageDefinition stage, IReadOnlyDictionary<string, double> finals, GradeScaleDefinition scale)
        {
            if (stage.Promotion.Count == 0 || finals.Count == 0)
            {
                return true;
            }

            int failed = finals.Values.Count(v => v < scale.Pass);
            double general = General(finals, scale) ?? scale.Max;
            return stage.Promotion.Any(r => failed <= r.Failed && (!r.MinAverage.HasValue || general >= r.MinAverage.Value));
        }

        /// <summary>
        /// El promedio del año que va (o, en vacaciones, del último que terminó), en la escala
        /// de 1 a 7; 0 si todavía no tiene notas.
        /// </summary>
        public static double CurrentAverageOnSeven(Character character, GradeScaleDefinition? scale)
        {
            SchoolRecord? record = character.SchoolRecord;
            if (record == null || scale == null)
            {
                return 0;
            }

            double? general = record.Marks.Count > 0
                ? General(Finals(record, scale), scale)
                : record.YearAverages.Count > 0 ? record.YearAverages[record.YearAverages.Count - 1] : (double?)null;
            return general.HasValue ? OnSevenScale(general.Value, scale) : 0;
        }

        /// <summary>En cuántos ramos va con promedio rojo (bajo la nota para aprobar) este año.</summary>
        public static int CurrentFailed(Character character, GradeScaleDefinition? scale)
        {
            SchoolRecord? record = character.SchoolRecord;
            return record == null || scale == null ? 0 : Finals(record, scale).Values.Count(v => v < scale.Pass);
        }

        /// <summary>
        /// Una nota en la escala de 1 a 7, para comparar entre países (los textos y los deseos
        /// usan esta: "un 6" es lo mismo en Chile que un 8,3 sobre 10).
        /// </summary>
        public static double OnSevenScale(double mark, GradeScaleDefinition scale) =>
            Math.Round(1.0 + ((mark - scale.Min) / (scale.Max - scale.Min) * 6.0), 1, MidpointRounding.AwayFromZero);
    }
}

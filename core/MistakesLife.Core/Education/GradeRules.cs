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

        /// <summary>Base de la nota de un ramo con dominio (D5a): la de quien no domina nada, con 0 en todo.</summary>
        public const double MasteryBase = 2.6;

        /// <summary>Lo que suma el dominio del ramo (0 a 100), como máximo.</summary>
        public const double MasteryWeight = 3.6;

        /// <summary>Lo que suma la inteligencia en un ramo con dominio, como máximo (el dominio ya la incluye en parte).</summary>
        public const double MasterySmartsWeight = 0.8;

        /// <summary>Lo que suma la constancia en un ramo con dominio, como máximo.</summary>
        public const double MasteryConscientiousnessWeight = 0.3;

        /// <summary>Bajo esta energía llega cansado a la prueba y rinde menos.</summary>
        public const int TiredEnergy = 30;

        /// <summary>Lo que resta, como máximo, llegar sin energía.</summary>
        public const double TiredPenalty = 0.6;

        /// <summary>
        /// Una nota en la escala del país. En un ramo que se estudia con preguntas (D5a) depende
        /// de cuánto lo domina, su inteligencia, su constancia, cuánto preparó esta prueba, su
        /// energía, su ánimo y algo de suerte: el jugador estudia, el personaje rinde. En los demás
        /// ramos depende de la inteligencia, la constancia y el estudio que deja su horario.
        /// </summary>
        public static double Mark(World world, ContentDatabase content, Character character, GradeScaleDefinition scale, string subjectId, int preparation, GameRandom random)
        {
            double luck = random.NextDouble() + random.NextDouble() + random.NextDouble() - 1.5;
            return Round(Scaled(OnSeven(world, content, character, subjectId, preparation) + luck, scale), scale);
        }

        /// <summary>
        /// La nota que se espera hoy en ese ramo, sin suerte (para decirle "te podría ir cerca de
        /// un 5,1"), en la escala del país.
        /// </summary>
        public static double Expected(World world, ContentDatabase content, Character character, GradeScaleDefinition scale, string subjectId, int preparation) =>
            Round(Scaled(OnSeven(world, content, character, subjectId, preparation), scale), scale);

        private static double OnSeven(World world, ContentDatabase content, Character character, string subjectId, int preparation)
        {
            double prepared = PreparationWeight * Math.Max(0, Math.Min(MaxPreparation, preparation));
            double sad = character.Stats.Happiness < SadHappiness ? SadPenalty : 0;
            int? mastery = StudyRules.Mastery(character, subjectId);
            if (mastery.HasValue)
            {
                double tired = character.Stats.Energy < TiredEnergy ? TiredPenalty * (TiredEnergy - character.Stats.Energy) / TiredEnergy : 0;
                return MasteryBase
                    + (MasteryWeight * mastery.Value / 100.0)
                    + (MasterySmartsWeight * character.Stats.Smarts / 100.0)
                    + (MasteryConscientiousnessWeight * character.Personality.Conscientiousness / 100.0)
                    + prepared - sad - tired;
            }

            double study = 0;
            if (character.Routine != null)
            {
                int mind = RoutineRules.Total(world, content, character, character.Routine, "mente");
                study = Math.Max(-0.5, Math.Min(1.0, mind / (double)FullStudy));
            }

            return BaseMark
                + (SmartsWeight * character.Stats.Smarts / 100.0)
                + (ConscientiousnessWeight * character.Personality.Conscientiousness / 100.0)
                + (StudyWeight * study)
                + prepared - sad;
        }

        private static double Scaled(double onSeven, GradeScaleDefinition scale)
        {
            double mark = Math.Max(1.0, Math.Min(7.0, onSeven));
            return scale.Min + ((mark - 1.0) / 6.0 * (scale.Max - scale.Min));
        }

        /// <summary>
        /// La probabilidad de faltar el día de la prueba por estar enfermo: más con la salud baja
        /// (con salud 80, 3 de cada 100 pruebas). Quien falta la rinde como recuperativa.
        /// </summary>
        public static double AbsentChance(Character character) =>
            Math.Max(0.01, (100 - character.Stats.Health) / 100.0 * 0.15);

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

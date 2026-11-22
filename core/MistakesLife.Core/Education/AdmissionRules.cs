using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// La PAES y la postulación a la universidad (D5a5), como en Chile: tres puntajes de 100 a
    /// 1.000 que se juntan según lo que pide cada carrera.
    /// <list type="bullet">
    /// <item>NEM: el promedio de notas de 1º a 4º medio, pasado a puntaje (4,0 = 100; 7,0 = 1.000).</item>
    /// <item>Ranking: igual al NEM hasta el promedio de un colegio típico; desde ahí sube hasta
    /// 1.000 con un 7,0 (premia destacar en tu colegio, como el ranking real).</item>
    /// <item>Cada prueba obligatoria (Competencia Lectora, Matemática): sale de lo que dominas
    /// del ramo que mide (tres cuartos) y tu inteligencia (un cuarto), más lo que te preparaste
    /// (un preuniversitario) y un poco de suerte del día.</item>
    /// </list>
    /// Para postular a la universidad, el promedio de las pruebas tiene que llegar al mínimo del
    /// país (458). Cada nivel de carrera con <c>admision</c> pondera los puntajes y pide su corte.
    /// Si la das otra vez, vale el mejor puntaje de cada prueba; el NEM y el ranking no cambian.
    /// </summary>
    public static class AdmissionRules
    {
        /// <summary>La preparación más alta que se le puede dar a la prueba (preuniversitario completo).</summary>
        public const int MaxPreparation = 3;

        /// <summary>Puntos que suma cada nivel de preparación en cada prueba.</summary>
        public const int PointsPerPreparation = 40;

        /// <summary>Puntos de más o de menos que da el día de la prueba (nervios, suerte).</summary>
        public const int DayNoise = 40;

        /// <summary>Cuánto pesa lo que dominas del ramo frente a tu inteligencia en cada prueba.</summary>
        public const double MasteryWeight = 0.75;

        /// <summary>
        /// Da la PAES: calcula el NEM y el ranking (solo la primera vez) y un puntaje por cada prueba,
        /// con <paramref name="preparation"/> de 0 (nada) a <see cref="MaxPreparation"/>. Guarda el
        /// mejor de cada prueba. No hace nada si su país no tiene admisión.
        /// </summary>
        public static void Take(World world, ContentDatabase content, Character character, int preparation)
        {
            if (character.Residence == null)
            {
                return;
            }

            CountryDefinition country = content.GetCountry(character.Residence.CountryId);
            AdmissionDefinition? admission = country.Admission;
            if (admission == null || country.GradeScale == null)
            {
                return;
            }

            SchoolRecord record = character.SchoolRecord ??= new SchoolRecord();
            if (record.AdmissionAttempts == 0)
            {
                double average = MediaAverage(character, country.GradeScale);
                record.AdmissionNem = Nem(average);
                record.AdmissionRanking = Ranking(record.AdmissionNem, average, admission.SchoolAverage);
            }

            GameRandom random = world.Random.Fork("admision.paes." + character.Id + "." + world.Date.TotalWeeks);
            int prep = Math.Max(0, Math.Min(MaxPreparation, preparation));
            record.LastAdmissionTests.Clear();
            foreach (AdmissionTestDefinition test in admission.Tests)
            {
                int score = TestScore(Known(character, record, test.SubjectId), character.Stats.Smarts, prep, random.NextInt(-DayNoise, DayNoise + 1));
                record.LastAdmissionTests[test.Id] = score;
                record.AdmissionTests[test.Id] = Math.Max(score, record.AdmissionTests.TryGetValue(test.Id, out int best) ? best : 0);
            }

            record.AdmissionAttempts++;
        }

        /// <summary>El puntaje NEM de un promedio de notas (en la escala de 1 a 7).</summary>
        public static int Nem(double average) => Clamp(100 + ((average - 4.0) / 3.0 * 900));

        /// <summary>
        /// El puntaje ranking: igual al NEM con notas hasta el promedio del colegio; sobre eso sube
        /// en línea recta hasta 1.000 con un 7,0.
        /// </summary>
        public static int Ranking(int nem, double average, double schoolAverage)
        {
            if (average <= schoolAverage)
            {
                return nem;
            }

            double above = Math.Min(1.0, (average - schoolAverage) / (7.0 - schoolAverage));
            return Clamp(nem + ((AdmissionDefinition.MaxScore - nem) * above));
        }

        /// <summary>Lo que sabe un alumno típico de cuarto medio (dominio e inteligencia): saca el puntaje medio.</summary>
        public const double TypicalKnowledge = 55;

        /// <summary>El puntaje medio de cada prueba (la escala de la PAES se centra cerca de 500).</summary>
        public const int MiddleScore = 500;

        /// <summary>Puntos por cada punto de lo que sabe sobre o bajo lo típico.</summary>
        public const int PointsPerKnowledge = 13;

        /// <summary>
        /// El puntaje de una prueba: lo que sabe (dominio e inteligencia) frente a un alumno típico,
        /// la preparación y el día.
        /// </summary>
        public static int TestScore(int mastery, int smarts, int preparation, int day)
        {
            double known = (MasteryWeight * mastery) + ((1 - MasteryWeight) * smarts);
            return Clamp(MiddleScore + (PointsPerKnowledge * (known - TypicalKnowledge)) + (PointsPerPreparation * preparation) + day);
        }

        /// <summary>
        /// El promedio de sus pruebas obligatorias (lo que se compara con el mínimo para postular);
        /// 0 si nunca dio la PAES.
        /// </summary>
        public static int ApplicationAverage(Character character, AdmissionDefinition admission)
        {
            SchoolRecord? record = character.SchoolRecord;
            if (record == null || record.AdmissionAttempts == 0 || admission.Tests.Count == 0)
            {
                return 0;
            }

            double sum = admission.Tests.Sum(t => record.AdmissionTests.TryGetValue(t.Id, out int s) ? s : 0);
            return (int)Math.Round(sum / admission.Tests.Count, MidpointRounding.AwayFromZero);
        }

        /// <summary>Si puede postular a la universidad: dio la PAES y el promedio de sus pruebas llega al mínimo.</summary>
        public static bool CanApply(Character character, AdmissionDefinition admission) =>
            ApplicationAverage(character, admission) >= admission.MinimumToApply;

        /// <summary>Su puntaje ponderado para ese nivel de carrera (0 si nunca dio la PAES o el nivel no pondera).</summary>
        public static double Weighted(Character character, CareerLevelDefinition level)
        {
            SchoolRecord? record = character.SchoolRecord;
            if (record == null || record.AdmissionAttempts == 0 || level.Admission == null)
            {
                return 0;
            }

            double total = 0;
            foreach (KeyValuePair<string, double> weight in level.Admission.Weights)
            {
                total += weight.Value * Score(record, weight.Key);
            }

            return Math.Round(total, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Si le alcanza para quedar en ese nivel de carrera: si el nivel no pide puntaje, sí; si
        /// pide, tiene que poder postular (mínimo del país) y su ponderado llegar al corte.
        /// </summary>
        public static bool Reaches(ContentDatabase content, Character character, string levelId)
        {
            CareerLevelDefinition level = content.GetCareerLevel(levelId);
            if (level.Admission == null)
            {
                return true;
            }

            AdmissionDefinition? admission = character.Residence == null ? null : content.GetCountry(character.Residence.CountryId).Admission;
            return admission != null && CanApply(character, admission) && Weighted(character, level) >= level.Admission.Cutoff;
        }

        /// <summary>Un puntaje por su nombre: "nem", "ranking" o el id de una prueba (0 si no lo tiene).</summary>
        public static int Score(SchoolRecord record, string name)
        {
            if (name == LevelAdmissionDefinition.Nem)
            {
                return record.AdmissionNem;
            }

            if (name == LevelAdmissionDefinition.Ranking)
            {
                return record.AdmissionRanking;
            }

            return record.AdmissionTests.TryGetValue(name, out int score) ? score : 0;
        }

        /// <summary>
        /// El promedio de sus últimos cuatro años del colegio (1º a 4º medio), contando el año que va
        /// si todavía tiene notas; 4,0 si no hay ninguno.
        /// </summary>
        public static double MediaAverage(Character character, GradeScaleDefinition scale)
        {
            SchoolRecord? record = character.SchoolRecord;
            if (record == null)
            {
                return 4.0;
            }

            var years = record.YearAverages.Select(a => GradeRules.OnSevenScale(a, scale)).ToList();
            if (record.Marks.Count > 0)
            {
                years.Add(GradeRules.CurrentAverageOnSeven(character, scale));
            }

            if (years.Count == 0)
            {
                return 4.0;
            }

            return years.Skip(Math.Max(0, years.Count - 4)).Average();
        }

        /// <summary>Lo que domina hoy del ramo que mide la prueba (o lo del año anterior; si no hay, su inteligencia).</summary>
        private static int Known(Character character, SchoolRecord record, string subjectId)
        {
            if (record.Mastery.TryGetValue(subjectId, out int now))
            {
                return now;
            }

            return record.LastMastery.TryGetValue(subjectId, out int last) ? last : character.Stats.Smarts;
        }

        private static int Clamp(double score) =>
            Math.Max(AdmissionDefinition.MinScore, Math.Min(AdmissionDefinition.MaxScore, (int)Math.Round(score, MidpointRounding.AwayFromZero)));
    }
}

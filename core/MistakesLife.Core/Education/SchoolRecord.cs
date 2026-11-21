using System.Collections.Generic;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Las notas del colegio de un personaje: las del año que va, los promedios de los años
    /// que terminó, cuántos años repitió y la prueba anunciada (la que se juega como momento).
    /// Lo lleva <see cref="GradeSystem"/>.
    /// </summary>
    public sealed class SchoolRecord
    {
        /// <summary>Las notas del año escolar que va, por ramo, en el orden en que llegaron.</summary>
        public SortedDictionary<string, List<double>> Marks { get; } = new SortedDictionary<string, List<double>>(System.StringComparer.Ordinal);

        /// <summary>El promedio final de cada año del colegio que terminó, en orden (sirve después para postular).</summary>
        public List<double> YearAverages { get; } = new List<double>();

        /// <summary>Las notas finales del último año que terminó, por ramo (la libreta de fin de año).</summary>
        public SortedDictionary<string, double> LastFinals { get; } = new SortedDictionary<string, double>(System.StringComparer.Ordinal);

        /// <summary>Cuántos años repitió: el colegio va ese número de años atrasado respecto de su edad.</summary>
        public int RepeatedYears { get; internal set; }

        /// <summary>El ramo de la prueba anunciada (o de la última que se devolvió); null si no hay.</summary>
        public string? TestSubjectId { get; internal set; }

        /// <summary>La semana (desde el inicio de la partida) en que se rinde la prueba anunciada.</summary>
        public int TestWeek { get; internal set; }

        /// <summary>Cuánto se preparó para la prueba anunciada (0 nada, 3 harto).</summary>
        public int TestPreparation { get; internal set; }

        /// <summary>La nota de la prueba anunciada, cuando ya se devolvió; null mientras no.</summary>
        public double? TestMark { get; internal set; }

        /// <summary>La semana en que se devolvió la prueba anunciada con su nota.</summary>
        public int TestReturnedWeek { get; internal set; }

        /// <summary>
        /// Si la prueba anunciada pasó a recuperativa: faltó ese día (enfermo) y la rinde la
        /// semana siguiente, como dice el reglamento de evaluación del colegio.
        /// </summary>
        public bool TestMakeup { get; internal set; }

        /// <summary>
        /// Cuánto domina cada ramo este año, de 0 a 100 (D5a): sube con las clases hasta lo que
        /// le da su base, sube más con cada acierto al estudiar y baja de a poco si deja de
        /// estudiar. Solo en los ramos con preguntas en su curso. Lo lleva <see cref="StudyRules"/>.
        /// </summary>
        public SortedDictionary<string, int> Mastery { get; } = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>Cuánto dominaba cada ramo al terminar el año anterior (la base del año que va).</summary>
        public SortedDictionary<string, int> LastMastery { get; } = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>El año escolar del dominio de este año (el de su curso); 0 si todavía no hay.</summary>
        public int MasteryYear { get; internal set; }

        /// <summary>La última semana en que estudió cada ramo este año (para el olvido).</summary>
        public SortedDictionary<string, int> LastStudiedWeek { get; } = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>Las preguntas que ya vio este año (no se repiten mientras queden otras).</summary>
        public SortedSet<string> SeenQuestions { get; } = new SortedSet<string>(System.StringComparer.Ordinal);

        /// <summary>Cuántas tardes estudió con preguntas este año.</summary>
        public int StudySessions { get; internal set; }

        /// <summary>Si la prueba anunciada todavía no se rinde.</summary>
        public bool TestPending => TestSubjectId != null && !TestMark.HasValue;
    }
}

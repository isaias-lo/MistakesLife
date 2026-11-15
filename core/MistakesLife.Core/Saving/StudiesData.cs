namespace MistakesLife.Core.Saving
{
    /// <summary>La carrera que se está estudiando, guardada.</summary>
    internal sealed class StudiesData
    {
        public string CareerId { get; set; } = string.Empty;

        public string LevelId { get; set; } = string.Empty;

        public string Institution { get; set; } = string.Empty;

        public int RequiredSemesters { get; set; }

        public int CompletedSemesters { get; set; }

        public int WeeksIntoSemester { get; set; }

        /// <summary>Desde la versión 26: notas de la universidad, el CFT o el instituto.</summary>
        public int GradedSemesters { get; set; }

        public int Preparation { get; set; }

        public int LastMarksSemester { get; set; }

        public System.Collections.Generic.SortedDictionary<string, double> LastMarks { get; set; } = new System.Collections.Generic.SortedDictionary<string, double>();

        public System.Collections.Generic.List<double> SemesterAverages { get; set; } = new System.Collections.Generic.List<double>();
    }
}

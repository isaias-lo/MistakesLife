using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Las notas del colegio, guardadas (desde la versión 23 del formato).</summary>
    internal sealed class SchoolRecordData
    {
        public Dictionary<string, List<double>>? Marks { get; set; }

        public List<double>? YearAverages { get; set; }

        public Dictionary<string, double>? LastFinals { get; set; }

        public int RepeatedYears { get; set; }

        public string? TestSubjectId { get; set; }

        public int TestWeek { get; set; }

        public int TestPreparation { get; set; }

        public double? TestMark { get; set; }

        public int TestReturnedWeek { get; set; }
    }
}

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

        // Desde la versión 42 (D5a): la recuperativa y el dominio de cada ramo.
        public bool TestMakeup { get; set; }

        public Dictionary<string, int>? Mastery { get; set; }

        public Dictionary<string, int>? LastMastery { get; set; }

        public int MasteryYear { get; set; }

        public Dictionary<string, int>? LastStudiedWeek { get; set; }

        public List<string>? SeenQuestions { get; set; }

        public int StudySessions { get; set; }
    }
}

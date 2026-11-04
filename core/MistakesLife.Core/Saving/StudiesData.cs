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
    }
}

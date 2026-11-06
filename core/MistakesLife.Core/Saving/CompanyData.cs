namespace MistakesLife.Core.Saving
{
    /// <summary>Una empresa guardada.</summary>
    internal sealed class CompanyData
    {
        public int Id { get; set; }

        public string CountryId { get; set; } = string.Empty;

        public string CareerId { get; set; } = string.Empty;

        public string SizeId { get; set; } = string.Empty;

        public string? NameId { get; set; }

        public int FoundedWeek { get; set; }

        /// <summary>Semana en que quebró, o null si sigue abierta.</summary>
        public int? ClosedWeek { get; set; }
    }
}

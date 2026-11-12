namespace MistakesLife.Core.Saving
{
    /// <summary>Una causa judicial guardada (desde la versión 14).</summary>
    internal sealed class CourtCaseData
    {
        public int Id { get; set; }

        public string TypeId { get; set; } = string.Empty;

        public string Rit { get; set; } = string.Empty;

        public int OwnerId { get; set; }

        public int? OtherPartyId { get; set; }

        public int OpenedWeek { get; set; }

        public int StageIndex { get; set; }

        public int StageStartedWeek { get; set; }

        public int? ClosedWeek { get; set; }

        public string? Outcome { get; set; }

        public decimal Amount { get; set; }
    }
}

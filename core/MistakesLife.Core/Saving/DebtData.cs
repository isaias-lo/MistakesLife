namespace MistakesLife.Core.Saving
{
    /// <summary>Una deuda guardada.</summary>
    internal sealed class DebtData
    {
        public string TypeId { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public int RemainingInstallments { get; set; }

        public int MonthsLate { get; set; }

        public double PriceIndex { get; set; }
    }
}

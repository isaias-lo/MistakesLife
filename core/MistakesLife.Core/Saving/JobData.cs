namespace MistakesLife.Core.Saving
{
    /// <summary>Un trabajo guardado.</summary>
    internal sealed class JobData
    {
        public string Kind { get; set; } = string.Empty;

        public decimal BaseSalary { get; set; }

        public double AdjustedPriceIndex { get; set; }

        public int MonthsSinceAdjustment { get; set; }
    }
}

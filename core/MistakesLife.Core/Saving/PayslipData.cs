using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Una liquidación de sueldo guardada.</summary>
    internal sealed class PayslipData
    {
        public int Week { get; set; }

        public string Kind { get; set; } = string.Empty;

        public decimal Gross { get; set; }

        public decimal Taxable { get; set; }

        public List<MoneyLineData> Lines { get; set; } = new List<MoneyLineData>();
    }
}

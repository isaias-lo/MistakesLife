using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>El cobro del seguro de desempleo, guardado (desde la versión 46 del formato).</summary>
    internal sealed class UnemploymentClaimData
    {
        public int StartWeek { get; set; }

        public bool HasRight { get; set; }

        public bool Solidarity { get; set; }

        public decimal SalaryBase { get; set; }

        public bool Finished { get; set; }

        public List<UnemploymentPaymentData> Payments { get; set; } = new List<UnemploymentPaymentData>();
    }
}

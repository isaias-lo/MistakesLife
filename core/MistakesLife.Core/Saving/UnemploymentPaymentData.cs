namespace MistakesLife.Core.Saving
{
    /// <summary>Un pago del seguro de desempleo, guardado (desde la versión 46 del formato).</summary>
    internal sealed class UnemploymentPaymentData
    {
        public int Week { get; set; }

        public int Number { get; set; }

        public decimal Amount { get; set; }

        public decimal FromSolidarityFund { get; set; }
    }
}

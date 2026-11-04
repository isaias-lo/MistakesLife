namespace MistakesLife.Core.Saving
{
    /// <summary>Una declaración anual de impuestos guardada, que el jugador aún no acepta.</summary>
    internal sealed class TaxReturnData
    {
        public int Year { get; set; }

        public decimal TaxableIncome { get; set; }

        public decimal Tax { get; set; }

        public decimal Contributions { get; set; }

        public decimal Credits { get; set; }
    }
}

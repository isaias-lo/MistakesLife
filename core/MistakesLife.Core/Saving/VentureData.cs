namespace MistakesLife.Core.Saving
{
    /// <summary>El negocio con un socio, guardado (desde la versión 33 del formato).</summary>
    internal sealed class VentureData
    {
        public int PartnerId { get; set; }

        public bool Formal { get; set; }

        public decimal Contribution { get; set; }

        public int MonthsOpen { get; set; }

        public decimal LastShare { get; set; }

        public decimal TotalShare { get; set; }

        public double Luck { get; set; } = 1;
    }
}

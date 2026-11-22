namespace MistakesLife.Core.Saving
{
    /// <summary>Un contrato de arriendo guardado.</summary>
    internal sealed class LeaseData
    {
        public string TypeId { get; set; } = string.Empty;

        public string CommuneId { get; set; } = string.Empty;

        /// <summary>Arriendo en dinero de los paquetes; 0 si vino de un guardado viejo (toma el precio de la comuna).</summary>
        public decimal BaseRent { get; set; }

        public double AdjustedPriceIndex { get; set; }

        public int MonthsSinceAdjustment { get; set; }

        public int MonthsLeft { get; set; }

        public decimal Deposit { get; set; }

        /// <summary>Con quién comparte el arriendo, o null (desde la versión 32).</summary>
        public int? RoommateId { get; set; }
    }
}

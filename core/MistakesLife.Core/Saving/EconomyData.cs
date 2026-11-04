using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>La economía de un país guardada.</summary>
    internal sealed class EconomyData
    {
        public string CountryId { get; set; } = string.Empty;

        public string RegimeId { get; set; } = string.Empty;

        public string? PhaseId { get; set; }

        public double AnnualInflation { get; set; }

        public double PriceIndex { get; set; }

        /// <summary>Índices de precios de los últimos meses, del más antiguo al más nuevo.</summary>
        public List<double> IndexHistory { get; set; } = new List<double>();
    }
}

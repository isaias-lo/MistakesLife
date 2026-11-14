using MistakesLife.Core.Economy;

namespace MistakesLife.Core.Saving
{
    /// <summary>Un trabajo guardado.</summary>
    internal sealed class JobData
    {
        public string Kind { get; set; } = string.Empty;

        public decimal BaseSalary { get; set; }

        public double AdjustedPriceIndex { get; set; }

        public int MonthsSinceAdjustment { get; set; }

        /// <summary>Área de su carrera, o null si es un trabajo fuera de una carrera.</summary>
        public string? AreaId { get; set; }

        /// <summary>Cargo en esa área, o null.</summary>
        public string? PositionId { get; set; }

        public int MonthsInPosition { get; set; }

        /// <summary>Antigüedad con este empleador (desde la versión 16).</summary>
        public int MonthsInJob { get; set; }

        public int Performance { get; set; } = Job.StartingPerformance;

        /// <summary>Número de su empresa, o null (desde la versión 6).</summary>
        public int? CompanyId { get; set; }
    }
}

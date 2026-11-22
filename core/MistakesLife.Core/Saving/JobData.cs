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

        [System.ComponentModel.DefaultValue(Job.StartingPerformance)]
        public int Performance { get; set; } = Job.StartingPerformance;

        /// <summary>Cumplimiento de la meta del último mes, en un oficio (desde la versión 30).</summary>
        public int GoalPercent { get; set; }

        public int MonthsUnderGoal { get; set; }

        public int MonthsOnGoal { get; set; }

        public int Warnings { get; set; }

        public decimal VariablePay { get; set; }

        public decimal Tips { get; set; }

        public string? Challenge { get; set; }

        /// <summary>Días de vacaciones ganados y no tomados (desde la versión 31).</summary>
        public double VacationDays { get; set; }

        /// <summary>Semana de su último día si renunció con aviso, o null (desde la versión 31).</summary>
        public int? LeavingWeek { get; set; }

        /// <summary>Número de su empresa, o null (desde la versión 6).</summary>
        public int? CompanyId { get; set; }

        /// <summary>Las tareas del mes en la pega (desde la versión 49, D5b).</summary>
        public System.Collections.Generic.List<WorkTaskData> Tasks { get; set; } = new System.Collections.Generic.List<WorkTaskData>();
    }
}

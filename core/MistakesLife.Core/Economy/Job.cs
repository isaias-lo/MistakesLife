using System;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// El trabajo de un personaje: tipo y sueldo bruto mensual. El sueldo se
    /// guarda en dinero del inicio del mundo y se lleva a precios del último
    /// reajuste: entre reajustes, la inflación se come parte del sueldo.
    /// Si es un trabajo de su carrera, también tiene área, cargo y desempeño.
    /// </summary>
    public sealed class Job
    {
        /// <summary>Desempeño con que se empieza en un trabajo nuevo.</summary>
        public const int StartingPerformance = 50;

        private int _performance = StartingPerformance;

        public Job(JobKind kind, decimal baseSalary, double priceIndex)
            : this(kind, baseSalary, priceIndex, null)
        {
        }

        public Job(JobKind kind, decimal baseSalary, double priceIndex, CareerPosition? position)
        {
            if (baseSalary < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseSalary), "El sueldo no puede ser negativo.");
            }

            Kind = kind;
            BaseSalary = baseSalary;
            AdjustedPriceIndex = priceIndex;
            Position = position;
        }

        /// <summary>Área y cargo en su carrera, o null si es un trabajo fuera de una carrera.</summary>
        public CareerPosition? Position { get; }

        /// <summary>Meses en el cargo actual (la experiencia que pide un ascenso).</summary>
        public int MonthsInPosition { get; internal set; }

        /// <summary>
        /// Qué tan bien trabaja, de 0 a 100. Lo cambian las decisiones en los eventos y,
        /// poco a poco, su responsabilidad. Un ascenso pide un mínimo.
        /// </summary>
        public int Performance
        {
            get => _performance;
            set => _performance = Math.Max(0, Math.Min(100, value));
        }

        public JobKind Kind { get; }

        /// <summary>Sueldo bruto mensual en dinero del inicio del mundo.</summary>
        public decimal BaseSalary { get; }

        /// <summary>Índice de precios del último reajuste (o de cuando empezó).</summary>
        public double AdjustedPriceIndex { get; private set; }

        /// <summary>Meses desde el último reajuste.</summary>
        public int MonthsSinceAdjustment { get; private set; }

        /// <summary>Sueldo bruto mensual de hoy, en la moneda del país.</summary>
        public decimal Salary => Math.Round(BaseSalary * (decimal)AdjustedPriceIndex, MidpointRounding.AwayFromZero);

        /// <summary>Repone el estado del reajuste al cargar una partida.</summary>
        internal void Restore(double adjustedPriceIndex, int monthsSinceAdjustment)
        {
            AdjustedPriceIndex = adjustedPriceIndex;
            MonthsSinceAdjustment = monthsSinceAdjustment;
        }

        /// <summary>Pasa un mes; si ya tocaba, el sueldo se reajusta a los precios de hoy.</summary>
        internal void AdvanceMonth(double priceIndex, int adjustmentMonths)
        {
            MonthsSinceAdjustment++;
            if (MonthsSinceAdjustment >= adjustmentMonths)
            {
                AdjustedPriceIndex = priceIndex;
                MonthsSinceAdjustment = 0;
            }
        }
    }
}

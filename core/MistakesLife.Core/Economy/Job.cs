using System;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// El trabajo de un personaje: tipo y sueldo bruto mensual. El sueldo se
    /// guarda en dinero del inicio del mundo y se lleva a precios del último
    /// reajuste: entre reajustes, la inflación se come parte del sueldo.
    /// </summary>
    public sealed class Job
    {
        public Job(JobKind kind, decimal baseSalary, double priceIndex)
        {
            if (baseSalary < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseSalary), "El sueldo no puede ser negativo.");
            }

            Kind = kind;
            BaseSalary = baseSalary;
            AdjustedPriceIndex = priceIndex;
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

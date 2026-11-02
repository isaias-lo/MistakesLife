using System;
using MistakesLife.Core.Content;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Calcula la declaración anual con las reglas del país:
    /// renta = sueldos (ya sin cotizaciones) + honorarios − gastos presuntos − cotizaciones de honorarios;
    /// impuesto por tramos anuales; y se compara con lo ya pagado durante el año.
    /// </summary>
    public static class TaxReturnCalculator
    {
        public static TaxReturn Compute(AnnualIncome income, AnnualTaxDefinition rules, double priceIndex)
        {
            decimal expenses = income.FreelanceGross * (decimal)rules.FreelancePresumedExpenses;
            if (rules.FreelancePresumedExpensesCap.HasValue)
            {
                expenses = Math.Min(expenses, PayrollCalculator.ToToday(rules.FreelancePresumedExpensesCap.Value, priceIndex));
            }

            decimal contributions = Round(income.FreelanceGross * (decimal)rules.FreelanceContributionBase * (decimal)rules.FreelanceContributionRate);
            decimal taxable = Math.Max(0, income.EmployeeTaxable + income.FreelanceGross - expenses - contributions);
            decimal tax = PayrollCalculator.ProgressiveTax(taxable, rules.Brackets, priceIndex);
            decimal credits = income.TaxWithheld + income.FreelanceRetention;
            return new TaxReturn(income.Year, taxable, tax, contributions, credits);
        }

        private static decimal Round(decimal amount) => Math.Round(amount, MidpointRounding.AwayFromZero);
    }
}

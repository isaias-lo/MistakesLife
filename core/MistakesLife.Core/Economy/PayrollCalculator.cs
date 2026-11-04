using System;
using System.Collections.Generic;
using MistakesLife.Core.Content;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Calcula la liquidación de un mes con las reglas del país. Los topes y los
    /// tramos del paquete están en dinero del inicio del mundo y se llevan a
    /// precios de hoy con el índice de precios (como la UF y la UTM en Chile).
    /// </summary>
    public static class PayrollCalculator
    {
        public static Payslip Compute(GameDate date, JobKind kind, decimal gross, PayrollDefinition rules, double priceIndex)
        {
            var lines = new List<PayslipLine>();
            decimal taxable = gross;
            // El trabajo informal no tiene descuentos: nadie cotiza ni retiene nada.
            List<DeductionDefinition> deductions = kind == JobKind.Employee ? rules.Employee
                : kind == JobKind.Freelance ? rules.Freelance
                : new List<DeductionDefinition>();
            foreach (DeductionDefinition deduction in deductions)
            {
                decimal basis = deduction.Cap.HasValue ? Math.Min(gross, ToToday(deduction.Cap.Value, priceIndex)) : gross;
                decimal amount = Round(basis * (decimal)deduction.Rate);
                lines.Add(new PayslipLine(deduction.Id, amount));
                if (deduction.ReducesTax)
                {
                    taxable -= amount;
                }
            }

            if (kind == JobKind.Employee && rules.MonthlyTax.Count > 0)
            {
                decimal tax = ProgressiveTax(taxable, rules.MonthlyTax, priceIndex);
                if (tax > 0)
                {
                    lines.Add(new PayslipLine(Payslip.IncomeTaxId, tax));
                }
            }

            return new Payslip(date, kind, gross, lines, taxable);
        }

        /// <summary>
        /// Impuesto por tramos: cada parte del ingreso paga la tasa de su tramo
        /// (el que gana más no paga más por todo, solo por lo que pasa del tramo).
        /// </summary>
        public static decimal ProgressiveTax(decimal income, IReadOnlyList<TaxBracketDefinition> brackets, double priceIndex)
        {
            decimal tax = 0;
            for (int i = 0; i < brackets.Count; i++)
            {
                decimal from = ToToday(brackets[i].From, priceIndex);
                if (income <= from)
                {
                    break;
                }

                decimal to = i + 1 < brackets.Count ? ToToday(brackets[i + 1].From, priceIndex) : decimal.MaxValue;
                tax += (Math.Min(income, to) - from) * (decimal)brackets[i].Rate;
            }

            return Round(tax);
        }

        /// <summary>Lleva un monto en dinero del inicio del mundo a precios de hoy.</summary>
        public static decimal ToToday(decimal amount, double priceIndex) => Round(amount * (decimal)priceIndex);

        private static decimal Round(decimal amount) => Math.Round(amount, MidpointRounding.AwayFromZero);
    }
}

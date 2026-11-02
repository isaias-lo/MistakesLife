namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Lo que ganó un personaje en un año del juego y lo que ya le retuvieron,
    /// para la declaración anual de impuestos. Montos en la moneda de cada mes.
    /// </summary>
    public sealed class AnnualIncome
    {
        public AnnualIncome(int year)
        {
            Year = year;
        }

        public int Year { get; }

        /// <summary>Sueldos con contrato, menos las cotizaciones que rebajan impuesto.</summary>
        public decimal EmployeeTaxable { get; internal set; }

        /// <summary>Impuesto mensual que ya le descontaron en las liquidaciones.</summary>
        public decimal TaxWithheld { get; internal set; }

        /// <summary>Honorarios brutos (lo que dicen las boletas).</summary>
        public decimal FreelanceGross { get; internal set; }

        /// <summary>Lo que le retuvieron de las boletas de honorarios.</summary>
        public decimal FreelanceRetention { get; internal set; }

        /// <summary>Suma una liquidación del año.</summary>
        public void Add(Payslip payslip)
        {
            if (payslip.Kind == JobKind.Employee)
            {
                EmployeeTaxable += payslip.Taxable;
                TaxWithheld += payslip.Amount(Payslip.IncomeTaxId);
            }
            else
            {
                FreelanceGross += payslip.Gross;
                FreelanceRetention += payslip.Gross - payslip.Net;
            }
        }
    }
}

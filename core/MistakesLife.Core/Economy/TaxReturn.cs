namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// La declaración anual de un año: cuánto impuesto correspondía, cuánto se
    /// debe cotizar por los honorarios y cuánto ya se pagó. El saldo es positivo
    /// si el fisco devuelve plata y negativo si hay que pagar.
    /// </summary>
    public sealed class TaxReturn
    {
        public TaxReturn(int year, decimal taxableIncome, decimal tax, decimal contributions, decimal credits)
        {
            Year = year;
            TaxableIncome = taxableIncome;
            Tax = tax;
            Contributions = contributions;
            Credits = credits;
        }

        /// <summary>Año de los ingresos que se declaran.</summary>
        public int Year { get; }

        /// <summary>Renta sobre la que se calcula el impuesto.</summary>
        public decimal TaxableIncome { get; }

        /// <summary>Impuesto anual según los tramos.</summary>
        public decimal Tax { get; }

        /// <summary>Cotizaciones de los honorarios (se pagan con lo retenido).</summary>
        public decimal Contributions { get; }

        /// <summary>Lo que ya se pagó durante el año: impuesto mensual y retenciones.</summary>
        public decimal Credits { get; }

        /// <summary>Positivo: devolución. Negativo: hay que pagar.</summary>
        public decimal Balance => Credits - Tax - Contributions;
    }
}

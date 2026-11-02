namespace MistakesLife.Core.Economy
{
    /// <summary>Una línea de la liquidación: un descuento (id con su nombre en lang/) y su monto.</summary>
    public sealed class PayslipLine
    {
        public PayslipLine(string id, decimal amount)
        {
            Id = id;
            Amount = amount;
        }

        /// <summary>Por ejemplo "descuento.chile.afp", o <see cref="Payslip.IncomeTaxId"/> para el impuesto.</summary>
        public string Id { get; }

        public decimal Amount { get; }
    }
}

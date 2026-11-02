namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Una deuda de un personaje: cuánto debe hoy, cuántas cuotas le quedan y
    /// cuántos meses seguidos lleva sin pagar.
    /// </summary>
    public sealed class Debt
    {
        public Debt(string typeId, decimal balance, int remainingInstallments, double priceIndex)
        {
            TypeId = typeId;
            Balance = balance;
            RemainingInstallments = remainingInstallments;
            PriceIndex = priceIndex;
        }

        /// <summary>Tipo de deuda del paquete del país, por ejemplo "deuda.chile.cae".</summary>
        public string TypeId { get; }

        /// <summary>Lo que debe hoy, a precios de hoy.</summary>
        public decimal Balance { get; set; }

        /// <summary>Cuotas que le quedan por pagar.</summary>
        public int RemainingInstallments { get; set; }

        /// <summary>Meses seguidos sin pagar la cuota.</summary>
        public int MonthsLate { get; set; }

        /// <summary>Índice de precios del último mes (para reajustar las deudas reajustables).</summary>
        public double PriceIndex { get; set; }
    }
}

using MistakesLife.Core.Time;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Un pago del seguro de desempleo que llegó a la cuenta del jugador: cuándo, cuánto
    /// (a los precios de ese día) y cuánto de eso puso el fondo común. Queda guardado
    /// con su número de pago para que el banco lo muestre como un movimiento con su descripción.
    /// </summary>
    public sealed class UnemploymentPayment
    {
        public UnemploymentPayment(GameDate date, int number, decimal amount, decimal fromSolidarityFund)
        {
            Date = date;
            Number = number;
            Amount = amount;
            FromSolidarityFund = fromSolidarityFund;
        }

        /// <summary>El día en que se depositó.</summary>
        public GameDate Date { get; }

        /// <summary>Qué pago fue: 1 el primero, 2 el segundo...</summary>
        public int Number { get; }

        /// <summary>Lo depositado, a los precios de ese día.</summary>
        public decimal Amount { get; }

        /// <summary>La parte que puso el fondo común porque la cuenta propia no alcanzó (0 si todo salió de la cuenta).</summary>
        public decimal FromSolidarityFund { get; }
    }
}

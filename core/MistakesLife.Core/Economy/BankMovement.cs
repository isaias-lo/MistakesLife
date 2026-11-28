using MistakesLife.Core.Time;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Una línea de la cartola: un peso que entró o salió de la cuenta, cuándo y por qué.
    /// Nada se anota sin una causa: cada movimiento viene de algo que el jugador decidió
    /// o de algo que le pasó (su sueldo, su arriendo, su compra, su deuda).
    /// </summary>
    public sealed class BankMovement
    {
        public BankMovement(GameDate date, string id, decimal amount, string detail)
        {
            Date = date;
            Id = id;
            Amount = amount;
            Detail = detail;
        }

        /// <summary>La semana en que pasó.</summary>
        public GameDate Date { get; }

        /// <summary>Qué fue (clave de idioma: movimiento.remuneracion, gasto.chile.arriendo...).</summary>
        public string Id { get; }

        /// <summary>Cuánto, a los precios de ese día: positivo si entró, negativo si salió.</summary>
        public decimal Amount { get; }

        /// <summary>De quién o de qué (un id de contenido, como la empresa o la deuda), o vacío.</summary>
        public string Detail { get; }
    }
}

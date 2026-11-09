using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// "El mes" de Tu dinero (docs/diseno-app.md): lo que costó el último mes contado en días
    /// de trabajo. Cada día de sueldo es un boleto; los días que se gastaron de más son
    /// prestados (los cubrió una deuda).
    /// </summary>
    public sealed class MonthView
    {
        /// <summary>Días hábiles de trabajo en un mes: el sueldo del mes se reparte entre ellos.</summary>
        public const int WorkDaysPerMonth = 22;

        /// <summary>En <see cref="Tickets"/>: un día de sueldo que quedó libre (no se gastó).</summary>
        public const int FreeTicket = -1;

        public MonthView(
            string headline,
            string payLabel,
            bool hasPay,
            IReadOnlyList<MonthExpenseView> expenses,
            IReadOnlyList<int> tickets,
            int borrowedDays,
            string footnote)
        {
            Headline = headline;
            PayLabel = payLabel;
            HasPay = hasPay;
            Expenses = expenses;
            Tickets = tickets;
            BorrowedDays = borrowedDays;
            Footnote = footnote;
        }

        /// <summary>La frase grande: cuántos días de trabajo costó el mes.</summary>
        public string Headline { get; }

        /// <summary>El sueldo líquido y cuánto vale un día de trabajo; vacío si no hay sueldo.</summary>
        public string PayLabel { get; }

        /// <summary>Si el jugador tuvo sueldo el último mes (si no, no hay boletos).</summary>
        public bool HasPay { get; }

        /// <summary>Los gastos del último mes, del más caro al más barato.</summary>
        public IReadOnlyList<MonthExpenseView> Expenses { get; }

        /// <summary>
        /// El talonario: un boleto por día. Cada número es la posición del gasto en
        /// <see cref="Expenses"/> que se llevó ese día, o <see cref="FreeTicket"/> si quedó libre.
        /// Los primeros <see cref="WorkDaysPerMonth"/> son días de sueldo; los que siguen son prestados.
        /// </summary>
        public IReadOnlyList<int> Tickets { get; }

        /// <summary>Días gastados de más, que cubrió una deuda.</summary>
        public int BorrowedDays { get; }

        /// <summary>Una frase debajo: los días libres o los prestados.</summary>
        public string Footnote { get; }
    }
}

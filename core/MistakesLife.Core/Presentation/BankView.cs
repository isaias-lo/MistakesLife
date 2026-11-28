using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Tu banco (D6-1b): la cuenta que abriste, la cartola mes a mes y lo que tienes guardado
    /// en el Seguro de Cesantía. Cada línea es un movimiento que de verdad pasó.
    /// </summary>
    public sealed class BankView
    {
        public BankView(string bankName, string accountLabel, string openedLabel, string balanceLabel, IReadOnlyList<BankMonthView> months, string unemploymentFundLabel)
        {
            BankName = bankName;
            AccountLabel = accountLabel;
            OpenedLabel = openedLabel;
            BalanceLabel = balanceLabel;
            Months = months;
            UnemploymentFundLabel = unemploymentFundLabel;
        }

        /// <summary>"Banco Nacional".</summary>
        public string BankName { get; }

        /// <summary>"Cuenta vista".</summary>
        public string AccountLabel { get; }

        /// <summary>"Abierta el 18 de marzo del año 27".</summary>
        public string OpenedLabel { get; }

        /// <summary>El saldo de hoy con su signo ("$1.248.300" o "-$86.400").</summary>
        public string BalanceLabel { get; }

        /// <summary>La cartola, del mes más nuevo al más viejo (como mucho dos años).</summary>
        public IReadOnlyList<BankMonthView> Months { get; }

        /// <summary>Lo que tienes en tu cuenta del Seguro de Cesantía a precios de hoy; vacío si no tienes nada ahí.</summary>
        public string UnemploymentFundLabel { get; }
    }
}

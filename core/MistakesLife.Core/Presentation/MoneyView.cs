using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// "Tu dinero" para la pantalla (docs/diseno-app.md): lo que tienes, la cartola del último mes
    /// y lo que debes. Regla de Isaias, nada se inventa: solo aparecen las líneas que de verdad
    /// se movieron (el sueldo que se pagó, los gastos que se cobraron, las cuotas de las deudas).
    /// </summary>
    public sealed class MoneyView
    {
        public MoneyView(string balanceLabel, IReadOnlyList<MoneyLineView> lines, string spentLabel, string daysLabel, IReadOnlyList<MoneyLineView> debts)
        {
            BalanceLabel = balanceLabel;
            Lines = lines;
            SpentLabel = spentLabel;
            DaysLabel = daysLabel;
            Debts = debts;
        }

        /// <summary>Lo que tienes hoy en la cuenta ("$120.000").</summary>
        public string BalanceLabel { get; }

        /// <summary>La cartola: primero lo que entró y después lo que salió. Vacía si nada se movió.</summary>
        public IReadOnlyList<MoneyLineView> Lines { get; }

        /// <summary>Cuánto salió en total el último mes ("-$480.000"); vacío si nada salió.</summary>
        public string SpentLabel { get; }

        /// <summary>"El mes te costó 22 días de sueldo"; vacío si no tienes sueldo.</summary>
        public string DaysLabel { get; }

        /// <summary>Lo que debes: una línea por deuda, con el saldo pendiente.</summary>
        public IReadOnlyList<MoneyLineView> Debts { get; }
    }
}

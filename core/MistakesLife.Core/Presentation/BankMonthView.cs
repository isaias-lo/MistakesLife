using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Un mes de la cartola: sus movimientos, lo que entró y lo que salió.</summary>
    public sealed class BankMonthView
    {
        public BankMonthView(string title, string inLabel, string outLabel, IReadOnlyList<MoneyLineView> lines)
        {
            Title = title;
            InLabel = inLabel;
            OutLabel = outLabel;
            Lines = lines;
        }

        /// <summary>"Marzo · Año 27".</summary>
        public string Title { get; }

        /// <summary>Lo que entró en el mes ("+$742.000"); vacío si no entró nada.</summary>
        public string InLabel { get; }

        /// <summary>Lo que salió en el mes ("-$480.000"); vacío si no salió nada.</summary>
        public string OutLabel { get; }

        /// <summary>Los movimientos del mes, del más nuevo al más viejo. La nota de cada uno es su fecha.</summary>
        public IReadOnlyList<MoneyLineView> Lines { get; }
    }
}

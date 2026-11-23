using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Lo que te entra al mes (D6-1a): cada ingreso real con su nombre (sueldo líquido, propinas,
    /// pensión, Seguro de Cesantía, pensión de alimentos, tu negocio) y el total. Solo lo que de
    /// verdad te llegó el último mes: nada se inventa.
    /// </summary>
    public sealed class IncomeView
    {
        public IncomeView(decimal total, string totalLabel, IReadOnlyList<MoneyLineView> lines)
        {
            Total = total;
            TotalLabel = totalLabel;
            Lines = lines;
        }

        /// <summary>El total del mes, en pesos de hoy.</summary>
        public decimal Total { get; }

        /// <summary>El total con signo ("+$742.000"); vacío si no te entra nada.</summary>
        public string TotalLabel { get; }

        /// <summary>Cada ingreso, del más grande al más chico.</summary>
        public IReadOnlyList<MoneyLineView> Lines { get; }
    }
}

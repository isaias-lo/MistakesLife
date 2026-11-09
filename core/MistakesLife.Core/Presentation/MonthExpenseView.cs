namespace MistakesLife.Core.Presentation
{
    /// <summary>Un gasto del mes, listo para mostrar: cuánto costó y cuántos días de trabajo fueron.</summary>
    public sealed class MonthExpenseView
    {
        public MonthExpenseView(string id, string label, string amountLabel, double days, string daysLabel)
        {
            Id = id;
            Label = label;
            AmountLabel = amountLabel;
            Days = days;
            DaysLabel = daysLabel;
        }

        /// <summary>Id del gasto en el paquete (por ejemplo "gasto.chile.arriendo").</summary>
        public string Id { get; }

        /// <summary>Nombre del gasto en el idioma del jugador.</summary>
        public string Label { get; }

        /// <summary>Lo que costó, con el signo de la moneda.</summary>
        public string AmountLabel { get; }

        /// <summary>Días de trabajo que costó (con decimales). 0 si el jugador no tiene sueldo.</summary>
        public double Days { get; }

        /// <summary>Los días en palabras ("3 días"); vacío si no tiene sueldo.</summary>
        public string DaysLabel { get; }
    }
}

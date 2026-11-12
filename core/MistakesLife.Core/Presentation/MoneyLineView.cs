namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Una línea de la cartola del banco (docs/diseno-app.md, "Tu dinero"): lo que entró o salió
    /// de la cuenta el último mes, con el nombre de la decisión que lo causó ("Arriendo de la pieza",
    /// "Transporte: micro o colectivo") y cuántos días de sueldo es.
    /// </summary>
    public sealed class MoneyLineView
    {
        public MoneyLineView(string id, string label, string amountLabel, bool isIncome, string note)
        {
            Id = id;
            Label = label;
            AmountLabel = amountLabel;
            IsIncome = isIncome;
            Note = note;
        }

        /// <summary>El id de lo que la causó (el contrato, la compra, el servicio, la deuda...).</summary>
        public string Id { get; }

        /// <summary>El nombre de la línea en el idioma del jugador.</summary>
        public string Label { get; }

        /// <summary>El monto con signo: "+$650.000" si entró, "-$180.000" si salió.</summary>
        public string AmountLabel { get; }

        /// <summary>Si es plata que entró (el sueldo) y no un gasto.</summary>
        public bool IsIncome { get; }

        /// <summary>Una nota chica debajo: "6 días de sueldo" en un gasto, "cuotas atrasadas: 2" en una deuda; vacía si no hay nada que decir.</summary>
        public string Note { get; }
    }
}

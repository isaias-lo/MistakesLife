namespace MistakesLife.Core.Presentation
{
    /// <summary>Una fila de tu ficha: qué es ("Trabajo"), lo principal ("Analista contable") y un detalle chico.</summary>
    public sealed class ProfileRowView
    {
        public ProfileRowView(string id, string label, string value, string detail)
        {
            Id = id;
            Label = label;
            Value = value;
            Detail = detail;
        }

        /// <summary>Qué fila es ("trabajo", "estudios", "vive", "pareja", "hijos", "salud", "colegio").</summary>
        public string Id { get; }

        /// <summary>El nombre de la fila en el idioma del jugador.</summary>
        public string Label { get; }

        /// <summary>Lo principal.</summary>
        public string Value { get; }

        /// <summary>Un detalle debajo; vacío si no hay.</summary>
        public string Detail { get; }
    }
}

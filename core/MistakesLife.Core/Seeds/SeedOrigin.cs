namespace MistakesLife.Core.Seeds
{
    /// <summary>
    /// De dónde viene una semilla: el momento en que se plantó (qué evento, qué elegiste,
    /// dónde y con quién). Sirve para mostrar "Esto viene de acá" cuando vuelve.
    /// </summary>
    public sealed class SeedOrigin
    {
        public SeedOrigin(string eventId, string? optionTextKey, string? placeKey, int? personId)
        {
            EventId = eventId;
            OptionTextKey = optionTextKey;
            PlaceKey = placeKey;
            PersonId = personId;
        }

        /// <summary>El evento en que se plantó.</summary>
        public string EventId { get; }

        /// <summary>El texto de la opción que eligió (null si se plantó sin elegir).</summary>
        public string? OptionTextKey { get; }

        /// <summary>El lugar del momento ("lugar.plaza"), si tenía.</summary>
        public string? PlaceKey { get; }

        /// <summary>La persona con quien pasó, si había una.</summary>
        public int? PersonId { get; }
    }
}

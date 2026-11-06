namespace MistakesLife.Core.Saving
{
    /// <summary>Una opción que el jugador eligió antes de volver al pasado.</summary>
    internal sealed class IntuitionData
    {
        public string EventId { get; set; } = string.Empty;

        public int CharacterId { get; set; }

        public int Week { get; set; }

        public int Option { get; set; }
    }
}

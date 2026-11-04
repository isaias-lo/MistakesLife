namespace MistakesLife.Core.Saving
{
    /// <summary>Un evento ya vivido, guardado.</summary>
    internal sealed class EventRecordData
    {
        public int Week { get; set; }

        public string EventId { get; set; } = string.Empty;

        public int CharacterId { get; set; }

        public int? ChosenOption { get; set; }
    }
}

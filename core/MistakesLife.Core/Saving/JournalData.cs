namespace MistakesLife.Core.Saving
{
    /// <summary>Una línea del diario guardada.</summary>
    internal sealed class JournalData
    {
        public int Week { get; set; }

        public int CharacterId { get; set; }

        public string TextKey { get; set; } = string.Empty;
    }
}

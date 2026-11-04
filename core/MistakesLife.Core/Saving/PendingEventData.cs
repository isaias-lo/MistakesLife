using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Un evento que espera la decisión del jugador, guardado.</summary>
    internal sealed class PendingEventData
    {
        public string EventId { get; set; } = string.Empty;

        public int CharacterId { get; set; }

        public int Week { get; set; }

        public int? SeedId { get; set; }

        public List<int> AvailableOptions { get; set; } = new List<int>();
    }
}

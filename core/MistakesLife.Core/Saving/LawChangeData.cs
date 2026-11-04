namespace MistakesLife.Core.Saving
{
    /// <summary>Una ley que cambió durante la partida.</summary>
    internal sealed class LawChangeData
    {
        public string CountryId { get; set; } = string.Empty;

        public string LawId { get; set; } = string.Empty;

        /// <summary>Tipo del valor: "bool", "number" o "text".</summary>
        public string Kind { get; set; } = string.Empty;

        public bool? Bool { get; set; }

        public double? Number { get; set; }

        public string? Text { get; set; }
    }
}

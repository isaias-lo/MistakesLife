namespace MistakesLife.Core.Saving
{
    /// <summary>Algo que quiere (o quiso) el personaje, guardado (desde la versión 21 del formato).</summary>
    internal sealed class WishData
    {
        public string WishId { get; set; } = string.Empty;

        public string StageKey { get; set; } = string.Empty;

        public int SinceWeek { get; set; }

        public int? PersonId { get; set; }

        public bool Highlighted { get; set; }

        public int State { get; set; }

        public int? ClosedWeek { get; set; }
    }
}

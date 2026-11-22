namespace MistakesLife.Core.Saving
{
    /// <summary>Un duelo del jugador, guardado (desde la versión 47 del formato).</summary>
    internal sealed class GriefData
    {
        public int PersonId { get; set; }

        public int SinceWeek { get; set; }

        public string Relation { get; set; } = string.Empty;

        public int Weight { get; set; }

        public int ReliefWeeks { get; set; }
    }
}

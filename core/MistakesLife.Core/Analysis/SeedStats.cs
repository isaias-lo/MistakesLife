namespace MistakesLife.Core.Analysis
{
    /// <summary>Conteo de semillas de un tipo en una simulación.</summary>
    public sealed class SeedStats
    {
        public int Planted { get; internal set; }

        public int Awakened { get; internal set; }

        public int Resolved { get; internal set; }

        public int Expired { get; internal set; }

        public int Open { get; internal set; }
    }
}

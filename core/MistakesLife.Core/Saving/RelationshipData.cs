namespace MistakesLife.Core.Saving
{
    /// <summary>Lo que un personaje siente por otro, guardado (desde la versión 3 del formato).</summary>
    internal sealed class RelationshipData
    {
        public int OtherId { get; set; }

        public int Affection { get; set; }

        public int Trust { get; set; }

        public int Respect { get; set; }

        public int Resentment { get; set; }

        /// <summary>Nombre del lazo en <see cref="Characters.RelationshipBond"/> ("None", "Friend"…).</summary>
        public string Bond { get; set; } = string.Empty;

        /// <summary>La semana de la última vez que hicieron algo juntos (desde la versión 50); null si nunca.</summary>
        public int? LastTogetherWeek { get; set; }
    }
}

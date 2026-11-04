using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Una semilla guardada, con los datos de su tipo tal como se plantó.</summary>
    internal sealed class SeedData
    {
        public int Id { get; set; }

        public string TypeId { get; set; } = string.Empty;

        public string Polarity { get; set; } = string.Empty;

        public int OwnerId { get; set; }

        public int PlantedWeek { get; set; }

        public int OwnerAgeAtPlanting { get; set; }

        public int? ParentSeedId { get; set; }

        public int ExpiresAfterWeeks { get; set; }

        public int MinWeeksDormant { get; set; }

        public bool Heritable { get; set; }

        public List<string> PossibleEvents { get; set; } = new List<string>();

        public List<int> Witnesses { get; set; } = new List<int>();

        public int Strength { get; set; }

        public string State { get; set; } = string.Empty;

        public int? AwakenedWeek { get; set; }

        public int? ClosedWeek { get; set; }
    }
}

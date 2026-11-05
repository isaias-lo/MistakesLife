using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Lo que se guarda de la máquina del tiempo (volver al pasado).</summary>
    internal sealed class TimeMachineData
    {
        public int Charges { get; set; }

        public bool Enabled { get; set; } = true;

        public int NextSnapshotId { get; set; } = 1;

        public List<string> UsedStages { get; set; } = new List<string>();

        public List<SnapshotData> Snapshots { get; set; } = new List<SnapshotData>();

        public List<IntuitionData> Intuitions { get; set; } = new List<IntuitionData>();
    }
}

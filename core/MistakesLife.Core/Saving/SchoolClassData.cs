using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>El curso del colegio, guardado (desde la versión 22 del formato).</summary>
    internal sealed class SchoolClassData
    {
        public string StageId { get; set; } = string.Empty;

        public int Level { get; set; }

        public string Letter { get; set; } = string.Empty;

        public int SchoolYear { get; set; }

        public int Students { get; set; }

        public int? TeacherId { get; set; }

        public List<int> ClassmateIds { get; set; } = new List<int>();

        public List<int> LeftIds { get; set; } = new List<int>();
    }
}

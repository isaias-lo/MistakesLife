using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Las tardes libres usadas en un mes, tal como se guardan (ver <see cref="Life.FreeTime"/>).</summary>
    public sealed class FreeTimeData
    {
        public int MonthKey { get; set; }

        public int Used { get; set; }

        public List<string> Done { get; set; } = new List<string>();
    }
}

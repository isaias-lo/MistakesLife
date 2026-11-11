using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Un celular guardado (desde la versión 12).</summary>
    internal sealed class PhoneData
    {
        public int SinceWeek { get; set; }

        public List<string> Apps { get; set; } = new List<string>();
    }
}

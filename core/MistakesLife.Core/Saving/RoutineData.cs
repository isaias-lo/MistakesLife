using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>El horario del jugador, guardado (desde la versión 19 del formato).</summary>
    internal sealed class RoutineData
    {
        public string StageKey { get; set; } = string.Empty;

        public bool Confirmed { get; set; }

        /// <summary>Los 14 bloques en orden (lunes tarde, lunes noche...); null = libre.</summary>
        public List<string?> Slots { get; set; } = new List<string?>();
    }
}

using System.Collections.Generic;

namespace MistakesLife.Core.Life
{
    /// <summary>Lo que pasó al hacer una acción: si la persona dijo que sí y lo que cambió de verdad.</summary>
    public sealed class ActionOutcome
    {
        public ActionOutcome(bool accepted, IReadOnlyList<ActionChange> changes)
        {
            Accepted = accepted;
            Changes = changes;
        }

        /// <summary>Si la persona dijo que sí (siempre, si la acción no lo pregunta).</summary>
        public bool Accepted { get; }

        /// <summary>Lo que cambió de verdad, en el orden de "cambia" (o de "si_no_acepta").</summary>
        public IReadOnlyList<ActionChange> Changes { get; }
    }
}

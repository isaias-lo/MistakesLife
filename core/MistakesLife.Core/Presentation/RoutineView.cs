using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// El horario del jugador, listo para dibujar: los 14 bloques y hacia dónde va su
    /// vida si sigue así ("Si sigo así: el ánimo sube, la mente se mantiene...").
    /// </summary>
    public sealed class RoutineView
    {
        public RoutineView(IReadOnlyList<RoutineSlotView> slots, bool confirmed, string mood, string mind, string health)
        {
            Slots = slots;
            Confirmed = confirmed;
            Mood = mood;
            Mind = mind;
            Health = health;
        }

        /// <summary>Lunes tarde, lunes noche, martes tarde...</summary>
        public IReadOnlyList<RoutineSlotView> Slots { get; }

        /// <summary>Si el jugador ya lo armó; si no, es una sugerencia y hay que mostrárselo.</summary>
        public bool Confirmed { get; }

        /// <summary>Hacia dónde va el ánimo: "sube", "baja" o "se mantiene".</summary>
        public string Mood { get; }

        public string Mind { get; }

        public string Health { get; }
    }
}

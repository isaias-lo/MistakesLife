using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Un evento listo para mostrar: su texto y las opciones que el jugador puede elegir.</summary>
    public sealed class EventView
    {
        public EventView(string eventId, string text, IReadOnlyList<string> options)
            : this(eventId, text, options, null)
        {
        }

        public EventView(string eventId, string text, IReadOnlyList<string> options, int? intuitionOption)
        {
            EventId = eventId;
            Text = text;
            Options = options;
            IntuitionOption = intuitionOption;
        }

        /// <summary>Id del evento (sirve para elegir una ilustración o una escena).</summary>
        public string EventId { get; }

        /// <summary>Texto del evento en el idioma del jugador.</summary>
        public string Text { get; }

        /// <summary>Opciones disponibles, en orden. Se responde con su posición en esta lista.</summary>
        public IReadOnlyList<string> Options { get; }

        /// <summary>
        /// La intuición después de volver al pasado: la posición (en <see cref="Options"/>)
        /// de lo que el jugador eligió la otra vez en este mismo momento. Null si es la primera vez.
        /// </summary>
        public int? IntuitionOption { get; }
    }
}

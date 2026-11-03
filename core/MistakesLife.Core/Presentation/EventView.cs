using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Un evento listo para mostrar: su texto y las opciones que el jugador puede elegir.</summary>
    public sealed class EventView
    {
        public EventView(string eventId, string text, IReadOnlyList<string> options)
        {
            EventId = eventId;
            Text = text;
            Options = options;
        }

        /// <summary>Id del evento (sirve para elegir una ilustración o una escena).</summary>
        public string EventId { get; }

        /// <summary>Texto del evento en el idioma del jugador.</summary>
        public string Text { get; }

        /// <summary>Opciones disponibles, en orden. Se responde con su posición en esta lista.</summary>
        public IReadOnlyList<string> Options { get; }
    }
}

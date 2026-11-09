using System.Collections.Generic;
using System.Linq;

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
            : this(eventId, text, options, intuitionOption, false)
        {
        }

        public EventView(string eventId, string text, IReadOnlyList<string> options, int? intuitionOption, bool isKeyMoment)
            : this(eventId, text, options, intuitionOption, isKeyMoment, false, null, null)
        {
        }

        public EventView(
            string eventId,
            string text,
            IReadOnlyList<string> options,
            int? intuitionOption,
            bool isKeyMoment,
            bool remembered,
            string? watching,
            IReadOnlyList<string>? noticedBy)
        {
            IsKeyMoment = isKeyMoment;
            Remembered = remembered;
            Watching = watching;
            NoticedBy = noticedBy ?? options.Select(_ => string.Empty).ToList();
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

        /// <summary>
        /// Si es un momento clave de la vida (<c>dia_clave</c> en el paquete): la pantalla lo
        /// destaca, porque lo que se elija aquí pesa más y se puede volver a él desde el futuro.
        /// </summary>
        public bool IsKeyMoment { get; }

        /// <summary>
        /// Si alguna opción deja una semilla: la pantalla le pone el sello "esto se recuerda",
        /// sin decir cuál opción es (eso se descubre años después).
        /// </summary>
        public bool Remembered { get; }

        /// <summary>
        /// Quiénes miran, ya en una frase ("Te miran: Pedro y Ana"): los testigos vivos de la
        /// semilla que despertó este evento. Null si nadie mira.
        /// </summary>
        public string? Watching { get; }

        /// <summary>
        /// Para cada opción (en el mismo orden que <see cref="Options"/>), quién lo va a notar,
        /// en una frase ("Lo va a notar Pedro"); vacío si nadie en particular.
        /// </summary>
        public IReadOnlyList<string> NoticedBy { get; }
    }
}

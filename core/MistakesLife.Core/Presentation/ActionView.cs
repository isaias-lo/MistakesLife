namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Un botón para algo que el jugador decide por su cuenta (un momento "a_pedido": pedir un
    /// aumento, tomar vacaciones, renunciar). Al apretarlo se llama a
    /// <see cref="Game.LifeSession.Request"/> con <see cref="EventId"/>.
    /// </summary>
    public sealed class ActionView
    {
        public ActionView(string eventId, string label)
        {
            EventId = eventId;
            Label = label;
        }

        /// <summary>El momento que se pide.</summary>
        public string EventId { get; }

        /// <summary>El texto del botón ("Pedir un aumento").</summary>
        public string Label { get; }
    }
}

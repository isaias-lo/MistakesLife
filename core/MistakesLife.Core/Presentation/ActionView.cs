namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Un botón para algo que el jugador decide por su cuenta (un momento "a_pedido": pedir un
    /// aumento, tomar vacaciones, renunciar). Al apretarlo se llama a
    /// <see cref="Game.LifeSession.Request"/> con <see cref="EventId"/>.
    /// </summary>
    public sealed class ActionView
    {
        public ActionView(string eventId, string label, bool opensBank = false)
        {
            EventId = eventId;
            Label = label;
            OpensBank = opensBank;
        }

        /// <summary>
        /// Si el momento es abrir tu cuenta (sus opciones abren una cuenta en un banco): la pantalla
        /// puede mostrar en su lugar los bancos para elegir (<see cref="LifePresenter.BankOffers"/>).
        /// </summary>
        public bool OpensBank { get; }

        /// <summary>El momento que se pide.</summary>
        public string EventId { get; }

        /// <summary>El texto del botón ("Pedir un aumento").</summary>
        public string Label { get; }
    }
}

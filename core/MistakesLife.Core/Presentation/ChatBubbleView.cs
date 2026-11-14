namespace MistakesLife.Core.Presentation
{
    /// <summary>Un mensaje de una conversación de Mensajito, ya con su texto.</summary>
    public sealed class ChatBubbleView
    {
        public ChatBubbleView(string text, bool fromPlayer, string day)
        {
            Text = text;
            FromPlayer = fromPlayer;
            Day = day;
        }

        public string Text { get; }

        /// <summary>Si lo mandó el jugador (va a la derecha, en verde).</summary>
        public bool FromPlayer { get; }

        /// <summary>Cuándo fue, como separador del día: "Esta semana", "Hace 2 meses".</summary>
        public string Day { get; }
    }
}

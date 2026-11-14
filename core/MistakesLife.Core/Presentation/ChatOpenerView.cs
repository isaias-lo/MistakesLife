namespace MistakesLife.Core.Presentation
{
    /// <summary>Algo que el jugador puede escribirle primero a alguien: su id (para <see cref="LifePresenter.WriteChat"/>) y el texto.</summary>
    public sealed class ChatOpenerView
    {
        public ChatOpenerView(string id, string text)
        {
            Id = id;
            Text = text;
        }

        public string Id { get; }

        public string Text { get; }
    }
}

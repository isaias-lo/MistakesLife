namespace MistakesLife.Core.Presentation
{
    /// <summary>Algo que viviste con una persona, para su ficha: cuándo ("A los 12") y qué pasó.</summary>
    public sealed class LivedView
    {
        public LivedView(string when, string text)
        {
            When = when;
            Text = text;
        }

        public string When { get; }

        public string Text { get; }
    }
}

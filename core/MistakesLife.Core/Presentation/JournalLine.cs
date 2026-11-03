namespace MistakesLife.Core.Presentation
{
    /// <summary>Una línea del diario lista para mostrar: a qué edad pasó y qué pasó.</summary>
    public sealed class JournalLine
    {
        public JournalLine(string ageLabel, string text)
        {
            AgeLabel = ageLabel;
            Text = text;
        }

        /// <summary>Edad del jugador cuando se escribió ("3 años", "12 semanas").</summary>
        public string AgeLabel { get; }

        public string Text { get; }
    }
}

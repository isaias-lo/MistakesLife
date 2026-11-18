namespace MistakesLife.Core.Presentation
{
    /// <summary>Una línea del diario lista para mostrar: cuándo y a qué edad pasó, y qué pasó.</summary>
    public sealed class JournalLine
    {
        public JournalLine(string ageLabel, string text, string dateLabel = "")
        {
            AgeLabel = ageLabel;
            Text = text;
            DateLabel = dateLabel;
        }

        /// <summary>El día en que pasó, como lo diría una persona ("14 de marzo"); vacío si no se sabe.</summary>
        public string DateLabel { get; }

        /// <summary>Edad del jugador cuando se escribió ("3 años", "12 semanas").</summary>
        public string AgeLabel { get; }

        public string Text { get; }
    }
}

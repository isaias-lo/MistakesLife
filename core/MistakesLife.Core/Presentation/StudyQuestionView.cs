using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>La pregunta que toca en una tarde de estudio (boceto D5aPregunta), con sus alternativas barajadas.</summary>
    public sealed class StudyQuestionView
    {
        public StudyQuestionView(int number, int total, string caption, string text, IReadOnlyList<string> options, int mastery, LifeBarLevel level)
        {
            Number = number;
            Total = total;
            Caption = caption;
            Text = text;
            Options = options;
            Mastery = mastery;
            Level = level;
        }

        /// <summary>Cuál es, desde 1.</summary>
        public int Number { get; }

        public int Total { get; }

        /// <summary>"Pregunta 1 de 3 · estudias sola".</summary>
        public string Caption { get; }

        public string Text { get; }

        public IReadOnlyList<string> Options { get; }

        public int Mastery { get; }

        public LifeBarLevel Level { get; }
    }
}

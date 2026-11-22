namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Cómo salió una tarea de la pega resuelta con lo que sabes (D5b2): cuántas acertaste, lo que
    /// dice tu jefa y cómo quedó la barra.
    /// </summary>
    public sealed class TaskQuizResultView
    {
        public TaskQuizResultView(string title, string score, string verdict, int change, int performance, string barTitle, string phrase)
        {
            Title = title;
            Score = score;
            Verdict = verdict;
            Change = change;
            Performance = performance;
            BarTitle = barTitle;
            Phrase = phrase;
        }

        /// <summary>El nombre de la tarea ("Declarar el IVA (F29)").</summary>
        public string Title { get; }

        /// <summary>"2 de 3 buenas".</summary>
        public string Score { get; }

        /// <summary>Cómo salió, en una frase ("Te luciste: tu jefa lo notó").</summary>
        public string Verdict { get; }

        /// <summary>Cuánto se movió la barra de cómo te ve tu jefa.</summary>
        public int Change { get; }

        /// <summary>La barra de 0 a 100 después de la tarea.</summary>
        public int Performance { get; }

        /// <summary>"Cómo te ve tu jefa" o "Cómo te ve tu jefe".</summary>
        public string BarTitle { get; }

        /// <summary>La frase de la barra ("Confía en ti para lo difícil").</summary>
        public string Phrase { get; }
    }
}

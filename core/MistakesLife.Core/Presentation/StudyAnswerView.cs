namespace MistakesLife.Core.Presentation
{
    /// <summary>Lo que pasó al responder (bocetos D5aPregunta y D5aError): si acertó, por qué, y qué dijo quien estudia contigo.</summary>
    public sealed class StudyAnswerView
    {
        public StudyAnswerView(bool correct, int chosen, int correctPosition, string verdict, string explanation, string? partnerLine, int mastery, LifeBarLevel level, int gain, bool hasNext)
        {
            Correct = correct;
            Chosen = chosen;
            CorrectPosition = correctPosition;
            Verdict = verdict;
            Explanation = explanation;
            PartnerLine = partnerLine;
            Mastery = mastery;
            Level = level;
            Gain = gain;
            HasNext = hasNext;
        }

        public bool Correct { get; }

        /// <summary>La posición que eligió.</summary>
        public int Chosen { get; }

        /// <summary>La posición de la correcta (se pinta verde).</summary>
        public int CorrectPosition { get; }

        /// <summary>"¡Bien!" o "Casi.".</summary>
        public string Verdict { get; }

        /// <summary>Por qué es así (si la profe te ayuda, lo explica ella).</summary>
        public string Explanation { get; }

        /// <summary>Lo que dijo tu compañera o compañero; null si estudias sin nadie de tu curso.</summary>
        public string? PartnerLine { get; }

        /// <summary>El dominio del ramo después de responder.</summary>
        public int Mastery { get; }

        public LifeBarLevel Level { get; }

        /// <summary>Cuánto subió con esta respuesta.</summary>
        public int Gain { get; }

        /// <summary>Si queda otra pregunta.</summary>
        public bool HasNext { get; }
    }
}

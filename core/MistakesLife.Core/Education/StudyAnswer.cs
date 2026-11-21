namespace MistakesLife.Core.Education
{
    /// <summary>Lo que pasó al responder una pregunta de una tarde de estudio.</summary>
    public sealed class StudyAnswer
    {
        public StudyAnswer(int chosen, bool correct, int correctPosition, int masteryGain, bool? companionCorrect)
        {
            Chosen = chosen;
            Correct = correct;
            CorrectPosition = correctPosition;
            MasteryGain = masteryGain;
            CompanionCorrect = companionCorrect;
        }

        /// <summary>La posición que eligió.</summary>
        public int Chosen { get; }

        public bool Correct { get; }

        /// <summary>En qué posición estaba la correcta.</summary>
        public int CorrectPosition { get; }

        /// <summary>Cuánto subió el dominio del ramo con esta respuesta (0 si falló sin que nadie le explicara).</summary>
        public int MasteryGain { get; }

        /// <summary>Si la persona con quien estudia la acertó; null si estudia sin nadie.</summary>
        public bool? CompanionCorrect { get; }
    }
}

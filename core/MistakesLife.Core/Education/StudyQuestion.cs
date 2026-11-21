using System.Collections.Generic;
using MistakesLife.Core.Content;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Una pregunta de una tarde de estudio, con sus alternativas ya barajadas y, si estudia con
    /// alguien, si esa persona la acierta (se decide al empezar, con el azar de la partida).
    /// </summary>
    public sealed class StudyQuestion
    {
        public StudyQuestion(QuestionDefinition definition, IReadOnlyList<int> order, bool? companionCorrect)
        {
            Definition = definition;
            Order = order;
            CompanionCorrect = companionCorrect;
        }

        public QuestionDefinition Definition { get; }

        /// <summary>En qué orden se muestran las alternativas: la posición 0 muestra la alternativa Order[0] ("a" es 0).</summary>
        public IReadOnlyList<int> Order { get; }

        /// <summary>Si la compañera o el compañero la acierta; null si estudia sin nadie.</summary>
        public bool? CompanionCorrect { get; }

        /// <summary>En qué posición se ve la correcta.</summary>
        public int CorrectPosition
        {
            get
            {
                for (int i = 0; i < Order.Count; i++)
                {
                    if (Order[i] == Definition.CorrectIndex)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }
    }
}

using System.Collections.Generic;
using MistakesLife.Core.Education;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Resolver una tarea con lo que sabes (D5b2): tres preguntas reales de los temas de la
    /// carrera que usa esa tarea (el IVA, las cotizaciones, la entrevista). Se responden una a
    /// una, como en una tarde de estudio, y al terminar la tarea sale según los aciertos
    /// (<see cref="WorkTaskRules.KnowResult"/>).
    /// </summary>
    public sealed class WorkQuiz
    {
        public WorkQuiz(string taskId, IReadOnlyList<StudyQuestion> questions)
        {
            TaskId = taskId;
            Questions = questions;
        }

        /// <summary>La tarea que se está resolviendo.</summary>
        public string TaskId { get; }

        public IReadOnlyList<StudyQuestion> Questions { get; }

        public List<StudyAnswer> Answers { get; } = new List<StudyAnswer>();

        /// <summary>La pregunta que va; null si ya respondió todas.</summary>
        public StudyQuestion? Current => Answers.Count < Questions.Count ? Questions[Answers.Count] : null;
    }
}

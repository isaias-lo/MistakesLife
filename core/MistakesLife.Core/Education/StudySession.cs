using System.Collections.Generic;
using MistakesLife.Core.Content;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Una tarde de estudio que va (D5a): el ramo, la unidad, el texto breve y sus tres preguntas,
    /// con quién estudia y lo que ha respondido. Vive solo mientras dura: la tarde se gasta al
    /// empezar y cada respuesta ya cambia el dominio del ramo, así que no hay que guardarla.
    /// </summary>
    public sealed class StudySession
    {
        public StudySession(string subjectId, QuestionUnitDefinition unit, IReadOnlyList<StudyQuestion> questions, int? companionId, bool withTeacher, int masteryBefore)
        {
            SubjectId = subjectId;
            Unit = unit;
            Questions = questions;
            CompanionId = companionId;
            WithTeacher = withTeacher;
            MasteryBefore = masteryBefore;
        }

        public string SubjectId { get; }

        /// <summary>La unidad del programa que está estudiando.</summary>
        public QuestionUnitDefinition Unit { get; }

        public IReadOnlyList<StudyQuestion> Questions { get; }

        /// <summary>La compañera o el compañero con quien estudia; null si estudia sin nadie de su curso.</summary>
        public int? CompanionId { get; }

        /// <summary>Si le pidió ayuda a su profe jefe: le explica lo que falla.</summary>
        public bool WithTeacher { get; }

        /// <summary>El dominio del ramo al empezar la tarde.</summary>
        public int MasteryBefore { get; }

        /// <summary>Lo que ha respondido, en orden.</summary>
        public List<StudyAnswer> Answers { get; } = new List<StudyAnswer>();

        /// <summary>La pregunta que toca responder; null si ya respondió todas.</summary>
        public StudyQuestion? Current => Answers.Count < Questions.Count ? Questions[Answers.Count] : null;

        /// <summary>Si ya terminó la tarde (con <see cref="StudyRules.Finish"/>).</summary>
        public bool Finished { get; internal set; }
    }
}

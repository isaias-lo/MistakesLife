using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Tu malla de la universidad, el CFT o el instituto: todos los semestres del plan, los
    /// aprobados, el que cursas (o repites) y los que faltan. Sale tal cual de lo que llevas.
    /// </summary>
    public sealed class StudyPlanView
    {
        public StudyPlanView(string career, string progress, IReadOnlyList<PlanSemesterView> semesters)
        {
            Career = career;
            Progress = progress;
            Semesters = semesters;
        }

        /// <summary>La carrera ("Ingeniería civil en computación").</summary>
        public string Career { get; }

        /// <summary>Cuánto llevas, en palabras ("Vas en el semestre 4 de 10 · llevas 5 cursados").</summary>
        public string Progress { get; }

        /// <summary>Los semestres del plan, del primero al último.</summary>
        public IReadOnlyList<PlanSemesterView> Semesters { get; }
    }
}

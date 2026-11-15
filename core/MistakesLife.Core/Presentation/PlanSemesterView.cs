using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Un semestre de tu malla: sus ramos y cómo va.</summary>
    public sealed class PlanSemesterView
    {
        public PlanSemesterView(int number, string name, PlanSemesterState state, IReadOnlyList<string> courses, IReadOnlyList<string> failedCourses)
        {
            Number = number;
            Name = name;
            State = state;
            Courses = courses;
            FailedCourses = failedCourses;
        }

        /// <summary>El número del semestre en el plan (1, 2, 3...).</summary>
        public int Number { get; }

        /// <summary>El semestre en palabras ("Semestre III").</summary>
        public string Name { get; }

        public PlanSemesterState State { get; }

        /// <summary>Los ramos del semestre, con su número ("Cálculo II").</summary>
        public IReadOnlyList<string> Courses { get; }

        /// <summary>Si lo repites: los ramos que quedaron rojos la vez pasada. Vacío si no.</summary>
        public IReadOnlyList<string> FailedCourses { get; }
    }
}

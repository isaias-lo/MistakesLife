using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Tu libreta de notas: los ramos del año que va (o, en vacaciones, las notas finales del
    /// año pasado) y el promedio general.
    /// </summary>
    public sealed class ReportCardView
    {
        public ReportCardView(IReadOnlyList<SubjectGradesView> subjects, string? general, bool finals, int repeatedYears)
        {
            Subjects = subjects;
            General = general;
            Finals = finals;
            RepeatedYears = repeatedYears;
        }

        /// <summary>Los ramos, en el orden de la etapa.</summary>
        public IReadOnlyList<SubjectGradesView> Subjects { get; }

        /// <summary>El promedio general ("5,9"); null si todavía no hay notas.</summary>
        public string? General { get; }

        /// <summary>Si son las notas finales del año que terminó (en vacaciones) y no las del año que va.</summary>
        public bool Finals { get; }

        /// <summary>Cuántos años repitió.</summary>
        public int RepeatedYears { get; }
    }
}

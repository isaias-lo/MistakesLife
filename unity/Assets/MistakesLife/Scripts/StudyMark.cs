#nullable enable

using MistakesLife.Core.Presentation;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Cómo iban los estudios antes de vivir la semana, para que al cerrar un semestre o un año
    /// suene lo que pasó: dos notas que suben si aprobaste, dos que bajan si repites. Solo mira
    /// lo que da el núcleo (la malla y la libreta).
    /// </summary>
    public readonly struct StudyMark
    {
        private readonly int _approved;
        private readonly bool _retaking;
        private readonly int _repeatedYears;
        private readonly bool _schoolFinals;
        private readonly bool _school;

        private StudyMark(int approved, bool retaking, int repeatedYears, bool schoolFinals, bool school)
        {
            _approved = approved;
            _retaking = retaking;
            _repeatedYears = repeatedYears;
            _schoolFinals = schoolFinals;
            _school = school;
        }

        public static StudyMark Of(LifePresenter presenter)
        {
            int approved = 0;
            bool retaking = false;
            StudyPlanView? plan = presenter.StudyPlan();
            if (plan != null)
            {
                foreach (PlanSemesterView semester in plan.Semesters)
                {
                    approved += semester.State == PlanSemesterState.Approved ? 1 : 0;
                    retaking |= semester.State == PlanSemesterState.Retaking;
                }
            }

            ReportCardView? card = presenter.ReportCard();
            bool school = card != null && !card.Semester;
            return new StudyMark(approved, retaking, school ? card!.RepeatedYears : 0, school && card!.Finals, school);
        }

        /// <summary>Toca el sonido del cambio, si lo hubo.</summary>
        public void PlayChange(StudyMark after)
        {
            if ((after._retaking && !_retaking) || (_school && after._school && after._repeatedYears > _repeatedYears))
            {
                Sounds.Play(Sound.Repeat);
            }
            else if (after._approved > _approved || (_school && after._school && after._schoolFinals && !_schoolFinals))
            {
                Sounds.Play(Sound.Approved);
            }
        }
    }
}

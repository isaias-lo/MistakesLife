using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Los datos de las pantallas de estudios: la malla y lo que viviste con cada compañero.</summary>
    public class StudyScreensTests
    {
        private const string Civil = "carrera.software.nivel.ingenieria_civil_computacion";

        private static ContentDatabase Content => RepoPacks.Content;

        private static LifeSession Studying()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, (18 * GameDate.WeeksPerYear) + 10 - world.Player.AgeInWeeks(world.Date), out _);
            Scripting.Effect.Parse($"estudiar('{Civil}')").Apply(new EventScope(world, Content, world.Player, null, readOnly: false));
            return session;
        }

        private static WeekCycle University() =>
            new WeekCycle().Register(new CareerSystem()).Register(new UniversitySystem(Content));

        [Fact]
        public void Malla_AlEmpezar_TieneTodosLosSemestresYElPrimeroSeCursa()
        {
            LifeSession session = Studying();
            University().AdvanceWeeks(session.World, 1, out _);

            StudyPlanView plan = new LifePresenter(session).StudyPlan()!;

            Assert.Equal(session.World.Player.Studies!.RequiredSemesters, plan.Semesters.Count);
            Assert.Equal(PlanSemesterState.Current, plan.Semesters[0].State);
            Assert.Equal("Semestre I", plan.Semesters[0].Name);
            Assert.Contains("Cálculo I", plan.Semesters[0].Courses);
            Assert.All(plan.Semesters.Skip(1), s => Assert.Equal(PlanSemesterState.Future, s.State));
            Assert.All(plan.Semesters, s => Assert.NotEmpty(s.Courses));
        }

        [Fact]
        public void Malla_DespuesDelPrimerSemestre_LoApruebaOLoRepiteConSusRamosRojos()
        {
            LifeSession session = Studying();
            University().AdvanceWeeks(session.World, CareerStudies.WeeksPerSemester + 1, out _);

            StudyPlanView plan = new LifePresenter(session).StudyPlan()!;
            CareerStudies studies = session.World.Player.Studies!;

            if (studies.CompletedSemesters >= 1)
            {
                Assert.Equal(PlanSemesterState.Approved, plan.Semesters[0].State);
                Assert.Equal(PlanSemesterState.Current, plan.Semesters[1].State);
            }
            else
            {
                Assert.Equal(PlanSemesterState.Retaking, plan.Semesters[0].State);
                Assert.NotEmpty(plan.Semesters[0].FailedCourses);
            }

            Assert.Contains("1 cursados", plan.Progress);
        }

        [Fact]
        public void Seccion_QuienSeFue_SeVeAlFinalConSeFueYSeGuarda()
        {
            LifeSession session = Studying();
            World world = session.World;
            University().AdvanceWeeks(world, 1, out _);
            for (int semester = 0; semester < 10 && world.Player.SchoolClass!.LeftIds.Count == 0; semester++)
            {
                University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);
            }

            var left = world.Player.SchoolClass!.LeftIds.ToList();
            Assert.NotEmpty(left);
            SchoolClassView view = new LifePresenter(session).SchoolClass()!;
            Assert.All(view.Left, m => Assert.Equal("Se fue", m.Bond));
            Assert.Equal(left, view.Left.Select(m => m.Id).ToList());
            Assert.DoesNotContain(view.Classmates, m => left.Contains(m.Id));

            World loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes()).World;
            Assert.Equal(left, loaded.Player.SchoolClass!.LeftIds);
        }

        [Fact]
        public void Malla_SinEstudiar_NoHay()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            Assert.Null(new LifePresenter(session).StudyPlan());
        }

        [Fact]
        public void Momento_ConUnaPersona_QuedaEnElHistorialYSeVeEnTuCursoYSeGuarda()
        {
            // Juega vidas hasta que un momento con un compañero de curso quede vivido.
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var session = new LifeSession(Content, seed, "pais.chile");
                session.TimeMachine.Enabled = false;
                var presenter = new LifePresenter(session);
                while (session.World.Player.IsAlive && session.PlayerAgeInYears < 16)
                {
                    if (session.Pending == null)
                    {
                        session.Advance(1);
                        continue;
                    }

                    int? person = session.Pending.PersonId;
                    string eventId = session.Pending.EventId;
                    session.Choose(session.Pending.AvailableOptions[0]);
                    if (person == null)
                    {
                        continue;
                    }

                    EventRecord record = session.World.EventHistory.Last(r => r.EventId == eventId);
                    Assert.Equal(person, record.PersonId);
                    ClassmateView? mate = presenter.SchoolClass()?.Classmates.FirstOrDefault(c => c.Id == person);
                    if (mate == null || record.ChosenOption == null)
                    {
                        continue;
                    }

                    Assert.False(string.IsNullOrEmpty(mate.Memory));
                    Assert.DoesNotContain("{", mate.Memory);

                    World loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes()).World;
                    Assert.Equal(person, loaded.EventHistory.Last(r => r.EventId == eventId).PersonId);
                    return;
                }
            }

            Assert.Fail("ninguna vida vivió un momento con un compañero de curso");
        }
    }
}

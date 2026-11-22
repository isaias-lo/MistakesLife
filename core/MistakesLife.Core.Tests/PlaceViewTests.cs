using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5c1: cada sección es un lugar que se toca (tu sala, tu aula y tu pega, con su gente real).</summary>
    public class PlaceViewTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida en Chile jugada hasta esa edad y semana del año (eligiendo la primera opción de cada momento).</summary>
        private static LifeSession SessionAt(int age, int weekOfYear, ulong seed = 3)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            int target = (age * GameDate.WeeksPerYear) + weekOfYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }

            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            return session;
        }

        /// <summary>Una vida en Chile a los 22 que entra a trabajar con contrato y ya armó su equipo.</summary>
        private static LifeSession Working()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (22 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, "jugador.trabajar(sueldo_minimo(), 'dependiente')");
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            session.Advance(1);
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            return session;
        }

        private static void Run(World world, string effect) =>
            Scripting.Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        [Fact]
        public void Sala_EnElColegio_TieneATuCursoYTuProfeConNombre()
        {
            LifeSession session = SessionAt(13, 16);
            var presenter = new LifePresenter(session);
            SchoolClassView cls = presenter.SchoolClass()!;

            PlaceView place = presenter.Place("estudios")!;

            Assert.Equal("lugar.sala", place.Id);
            Assert.True(place.IsClassroom);
            Assert.Contains(cls.Name, place.Title);
            Assert.True(place.Player.IsPlayer);
            Assert.Equal(cls.Classmates.Take(LifePresenter.MaxPlacePeople).Select(c => c.Id), place.People.Select(p => p.Id));
            Assert.All(place.People, p => Assert.False(string.IsNullOrWhiteSpace(p.Tag)));
            Assert.All(place.People, p => Assert.False(p.Lead));
            Character teacher = Education.SchoolRules.Teacher(session.World, session.World.Player)!;
            Assert.Equal(teacher.Id, place.Lead!.Id);
            Assert.True(place.Lead.Lead);
            Assert.Contains(teacher.FirstName, place.Lead.Tag);
            Assert.Empty(place.LeadActions);
        }

        [Fact]
        public void Sala_LaLibretaSeTocaSoloSiHayNotas()
        {
            LifeSession session = SessionAt(13, 16);
            var presenter = new LifePresenter(session);

            PlaceView place = presenter.Place("estudios")!;

            Assert.Equal(presenter.ReportCard() != null, place.Things.Any(t => t.Id == PlaceView.Notebook));
        }

        [Fact]
        public void Sala_LaPizarraMuestraLaPruebaQueViene()
        {
            LifeSession session = SessionAt(13, 16);
            var presenter = new LifePresenter(session);
            StudySubjectView? test = presenter.StudySubjects().FirstOrDefault(s => s.Upcoming != null);

            PlaceThingView? board = presenter.Place("estudios")!.Things.FirstOrDefault(t => t.Id == PlaceView.Board);

            if (test == null)
            {
                Assert.Null(board);
            }
            else
            {
                Assert.Contains(test.Name, board!.Label);
                Assert.Contains(test.Upcoming!, board.Label);
            }
        }

        [Fact]
        public void Lugar_SinColegioNiPega_NoHay()
        {
            LifeSession session = SessionAt(3, 10);
            var presenter = new LifePresenter(session);

            Assert.Null(presenter.Place("estudios"));
            Assert.Null(presenter.Place("trabajo"));
            Assert.Null(presenter.Place("gente"));
        }

        [Fact]
        public void Pega_TieneTuEquipoTuJefaYLoQueLePuedesPedir()
        {
            LifeSession session = Working();
            var presenter = new LifePresenter(session);
            TeamView team = presenter.Team()!;

            PlaceView place = presenter.Place("trabajo")!;

            Assert.Equal(LifePresenter.WorkplacePlace, place.Id);
            Assert.False(place.IsClassroom);
            Assert.NotNull(place.Workplace);
            Assert.Equal(team.Colleagues.Select(c => c.Id), place.People.Select(p => p.Id));
            Assert.Equal(WorkRules.Boss(session.World, session.World.Player)!.Id, place.Lead!.Id);
            Assert.Equal(team.Actions.Select(a => a.EventId), place.LeadActions.Select(a => a.EventId));
            Assert.All(place.People, p => Assert.Contains(p.Tag, team.Colleagues.First(c => c.Id == p.Id).Name));
        }

        [Fact]
        public void Pega_LaAgendaCuentaLasTareasPendientes()
        {
            LifeSession session = Working();
            var presenter = new LifePresenter(session);
            TeamView team = presenter.Team()!;

            PlaceThingView? agenda = presenter.Place("trabajo")!.Things.FirstOrDefault(t => t.Id == PlaceView.Agenda);

            Assert.Equal(team.Tasks.Count > 0, agenda != null);
        }

        [Fact]
        public void Lugar_AlCambiarLaGente_CambiaSuFirma()
        {
            LifeSession session = SessionAt(13, 16);
            var presenter = new LifePresenter(session);
            string before = presenter.Place("estudios")!.Signature;
            Assert.Equal(before, presenter.Place("estudios")!.Signature);

            session.World.Player.SchoolClass!.ClassmateIds.RemoveAt(0);

            Assert.NotEqual(before, presenter.Place("estudios")!.Signature);
        }
    }
}

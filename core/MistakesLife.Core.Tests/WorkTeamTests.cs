using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R6a: el trabajo por dentro (jefe y colegas con nombre).</summary>
    public class WorkTeamTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida en Chile a los 22 que entra a trabajar (con ese tipo de contrato).</summary>
        private static LifeSession Working(string kind = "dependiente", ulong seed = 3)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (22 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, $"jugador.trabajar(sueldo_minimo(), '{kind}')");
            return session;
        }

        private static void Run(World world, string effect) =>
            Scripting.Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static void Weeks(LifeSession session, int weeks)
        {
            for (int i = 0; i < weeks && session.World.Player.IsAlive; i++)
            {
                while (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }
        }

        [Fact]
        public void Equipo_AlEntrarATrabajar_TieneJefeYColegasConNombreQueSonTusColegas()
        {
            LifeSession session = Working();
            Weeks(session, 1);
            World world = session.World;
            Character player = world.Player;

            Assert.True(WorkRules.InTeam(player));
            var colleagues = WorkRules.Colleagues(world, player);
            Assert.Equal(WorkRules.TeamSize, colleagues.Count);
            Assert.Equal(colleagues.Count, colleagues.Select(c => c.FirstName).Distinct().Count());
            Character boss = WorkRules.Boss(world, player)!;
            Assert.InRange(boss.AgeInYears(world.Date), WorkRules.MinBossAge - 1, WorkRules.MaxBossAge + 1);
            Assert.All(colleagues.Append(boss), c => Assert.Equal(RelationshipBond.Colleague, RelationshipRules.Feelings(world, player, c).Bond));
            Assert.Contains(world.Journal, j => j.TextKey == "diario.pega.equipo_nuevo");

            TeamView view = new LifePresenter(session).Team()!;
            Assert.Equal(WorkRules.TeamSize, view.Colleagues.Count);
            Assert.NotNull(view.Boss);
        }

        [Fact]
        public void Equipo_AHonorarios_NoHay()
        {
            LifeSession session = Working("honorarios");
            Weeks(session, 2);
            Assert.False(WorkRules.InTeam(session.World.Player));
            Assert.Null(new LifePresenter(session).Team());
        }

        [Fact]
        public void Equipo_AlDejarElTrabajo_SeDeshaceYQuedanComoColegas()
        {
            LifeSession session = Working();
            Weeks(session, 1);
            World world = session.World;
            var team = WorkRules.Colleagues(world, world.Player).Select(c => c.Id).ToList();

            Run(world, "jugador.dejar_trabajo()");
            Weeks(session, 1);

            Assert.Null(world.Player.Team);
            Assert.All(team, id => Assert.Contains(RelationshipRules.WithBond(world, world.Player, RelationshipBond.Colleague).Concat(RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend)), c => c.Id == id));
        }

        [Fact]
        public void Equipo_ConLosAnos_AlguienSeVaYElEquipoNuncaQuedaVacio()
        {
            LifeSession session = Working();
            Weeks(session, 1);
            World world = session.World;
            var first = world.Player.Team!.ColleagueIds.ToList();
            bool someoneLeft = false;
            for (int year = 0; year < 6 && WorkRules.InTeam(world.Player); year++)
            {
                Weeks(session, GameDate.WeeksPerYear);
                if (!WorkRules.InTeam(world.Player))
                {
                    break;
                }

                someoneLeft |= world.Player.Team!.LeftIds.Count > 0;
                Assert.InRange(WorkRules.Colleagues(world, world.Player).Count, WorkRules.MinTeamSize, WorkRules.TeamSize);
            }

            Assert.True(someoneLeft || !first.SequenceEqual(world.Player.Team?.ColleagueIds ?? first), "en seis años nadie se fue del equipo");
        }

        [Fact]
        public void Guardado_ConservaElEquipo()
        {
            LifeSession session = Working();
            Weeks(session, 5);
            WorkTeam before = session.World.Player.Team!;

            WorkTeam after = LifeSession.LoadFromBytes(Content, session.SaveToBytes()).World.Player.Team!;

            Assert.Equal(before.BossId, after.BossId);
            Assert.Equal(before.ColleagueIds, after.ColleagueIds);
            Assert.Equal(before.LeftIds, after.LeftIds);
            Assert.Equal(before.MonthsInJob, after.MonthsInJob);
        }

        [Fact]
        public void Momento_ConJefe_LoNombraComoTuJefeOTuJefa()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                LifeSession session = Working(seed: seed);
                var presenter = new LifePresenter(session);
                for (int week = 0; week < 5 * GameDate.WeeksPerYear && session.World.Player.IsAlive; week++)
                {
                    if (session.Pending != null)
                    {
                        EventDefinition ev = Content.GetEvent(session.Pending.EventId);
                        if (ev.With == "jefe")
                        {
                            Character boss = WorkRules.Boss(session.World, session.World.Player)!;
                            Assert.Equal(boss.Id, session.Pending.PersonId);
                            Assert.Contains((boss.Gender == Gender.Female ? "tu jefa " : "tu jefe ") + boss.FirstName, presenter.Snapshot().PendingEvent!.Text);
                            return;
                        }

                        session.Choose(session.Pending.AvailableOptions[0]);
                        continue;
                    }

                    session.Advance(1);
                }
            }

            Assert.Fail("en ninguna vida hubo un momento con la jefatura");
        }

        [Fact]
        public void ColegaNuevo_LeLlegaAQuienTrabajaPorSuCuenta()
        {
            // Quien tiene equipo ya conoce a sus colegas por nombre; quien trabaja a honorarios
            // conoce gente del rubro de vez en cuando.
            var seen = CareerAreaLives.FreelanceWorkerEvents(RepoPacks.Content, lives: 4);

            Assert.Contains("evento.colega_nuevo", seen);
        }
    }
}

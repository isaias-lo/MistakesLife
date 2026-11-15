using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R6b: oficios sin estudios (vendedor, garzón, maestro) con metas, pago variable y desafíos.</summary>
    public class OccupationTests
    {
        private const string Vestuario = "oficio.vendedor.area.vestuario";
        private const string Restoran = "oficio.garzon.area.restoran";
        private const string ObraGruesa = "oficio.maestro.area.obra_gruesa";

        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida en Chile a los 22, sin título, que entra a trabajar en esa área de un oficio.</summary>
        private static LifeSession WorkingIn(string area, ulong seed = 5, int age = 22)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (age * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Assert.Empty(world.Player.Degrees);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, "jugador.desmarcar('buscando_trabajo')");
            Run(world, "jugador.marcar('trabajando')");
            Run(world, $"jugador.trabajar_en('{area}')");
            return session;
        }

        private static void Run(World world, string effect) =>
            Scripting.Effect.Parse(effect).Apply(Scope(world));

        private static Scripting.Value Eval(World world, string expression) =>
            Scripting.Expression.Parse(expression).Evaluate(Scope(world));

        private static EventScope Scope(World world) => new EventScope(world, Content, world.Player, null, readOnly: false);

        /// <summary>Avanza semanas eligiendo siempre la primera opción, o las de una lista de eventos.</summary>
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

        /// <summary>Avanza hasta justo después del cierre del próximo mes.</summary>
        private static void NextMonth(LifeSession session)
        {
            do
            {
                Weeks(session, 1);
            }
            while (!session.World.Date.AddWeeks(-1).IsFirstWeekOfMonth);
        }

        [Fact]
        public void Oficio_SinTitulo_SePuedeEntrarYElSueldoParteDelMinimo()
        {
            LifeSession session = WorkingIn(Vestuario);
            World world = session.World;

            Assert.Equal("oficio.vendedor", Eval(world, "jugador.oficio").AsText("prueba"));
            Assert.Equal("oficio.vendedor.cargo.vendedor", Eval(world, "jugador.cargo").AsText("prueba"));
            Assert.True(Eval(world, $"jugador.puede_trabajar_en('{Restoran}')").AsBool("prueba"));
            decimal minimum = Content.GetCountry("pais.chile").Economy!.Salaries!.Minimum;
            Assert.Equal(minimum, world.Player.Job!.BaseSalary);
        }

        [Fact]
        public void Oficio_EnArgentina_ElSueldoBaseEsElDeSuConvenio()
        {
            var session = new LifeSession(Content, 5, "pais.argentina");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (22 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, $"jugador.trabajar_en('{Vestuario}')");

            decimal minimum = Content.GetCountry("pais.argentina").Economy!.Salaries!.Minimum;
            Assert.Equal(System.Math.Round(minimum * 3.0m), world.Player.Job!.BaseSalary);
        }

        [Fact]
        public void Comision_SeCierraCadaMesYEntraEnElSueldoBrutoDeLaLiquidacion()
        {
            LifeSession session = WorkingIn(Vestuario);
            NextMonth(session);
            NextMonth(session);
            Job job = session.World.Player.Job!;

            Assert.InRange(job.GoalPercent, OccupationRules.MinPercent, OccupationRules.MaxPercent);
            Assert.True(job.VariablePay > 0);
            Assert.Equal(0, job.Tips);
            Assert.Equal(job.Salary + job.VariablePay, session.World.Player.LastPayslip!.Gross);
        }

        [Fact]
        public void Propinas_LleganATuDineroYNoEntranEnLaLiquidacion()
        {
            LifeSession session = WorkingIn(Restoran);
            NextMonth(session);
            NextMonth(session);
            Job job = session.World.Player.Job!;

            Assert.True(job.Tips > 0);
            Assert.Equal(0, job.VariablePay);
            Assert.Equal(job.Salary, session.World.Player.LastPayslip!.Gross);
            Assert.True(Eval(session.World, "jugador.pago_variable").AsNumber("prueba") > 0);
        }

        [Fact]
        public void Meta_ConDesempenoMedio_SeCumpleCasiSiempreYConMaloNo()
        {
            var random = new GameRandom(11);
            double Average(int performance) => Enumerable.Range(0, 2000).Average(_ => OccupationRules.MonthPercent(performance, random, 1));

            Assert.InRange(Average(50), 104, 108);
            Assert.True(Average(20) < OccupationRules.Goal);
            Assert.True(Average(80) > OccupationRules.ChallengeGoal);
            // En una crisis se vende menos.
            Assert.True(Enumerable.Range(0, 500).Average(_ => OccupationRules.MonthPercent(50, random, 0.8)) < OccupationRules.Goal);
        }

        [Fact]
        public void Desafio_SiCierraElMesSobreLaMetaAlta_GanaLaMarca()
        {
            LifeSession session = WorkingIn(Vestuario);
            NextMonth(session);
            Run(session.World, "jugador.desempeno = 100");
            Run(session.World, "jugador.desafio('desafio_vendedor_encargado')");
            Assert.True(Eval(session.World, "jugador.en_desafio").AsBool("prueba"));

            NextMonth(session);

            Assert.True(session.World.Player.HasFlag("desafio_vendedor_encargado"));
            Assert.Null(session.World.Player.Job!.Challenge);
            Assert.Contains(session.World.Journal, j => j.TextKey == OccupationRules.ChallengeWonJournal);
        }

        [Fact]
        public void Desafio_SiNoLlegaALaMetaAlta_NoGanaLaMarca()
        {
            LifeSession session = WorkingIn(ObraGruesa);
            NextMonth(session);
            Run(session.World, "jugador.desempeno = 0");
            Run(session.World, "jugador.desafio('desafio_maestro_primera')");

            NextMonth(session);

            Assert.False(session.World.Player.HasFlag("desafio_maestro_primera"));
            Assert.Contains(session.World.Journal, j => j.TextKey == OccupationRules.ChallengeLostJournal);
        }

        [Fact]
        public void BajoLaMeta_LlegaElLlamadoLuegoLaAmonestacionYAlFinalElDespido()
        {
            // A los 26 ya no corre el fin de contrato de los jóvenes (18 a 24), que podría cortar la pega antes.
            LifeSession session = WorkingIn(Vestuario, age: 26);
            World world = session.World;
            for (int month = 0; month < 14 && world.Player.Job != null; month++)
            {
                Run(world, "jugador.desempeno = 0");
                NextMonth(session);
            }

            var history = world.EventHistory.Select(r => r.EventId).ToList();
            int llamado = history.IndexOf("evento.oficio.llamado");
            int amonestacion = history.IndexOf("evento.oficio.amonestacion");
            int despido = history.IndexOf("evento.oficio.despido");
            Assert.True(llamado >= 0 && amonestacion > llamado && despido > amonestacion, string.Join(", ", history.Where(h => h.StartsWith("evento.oficio"))));
            Assert.Null(world.Player.Job);
        }

        [Fact]
        public void Amonestacion_MedioAnoCumpliendoLaMeta_QuedaAtras()
        {
            LifeSession session = WorkingIn(Vestuario);
            Run(session.World, "jugador.amonestaciones = 1");
            for (int month = 0; month < OccupationRules.MonthsToClearWarning + 1; month++)
            {
                Run(session.World, "jugador.desempeno = 100");
                NextMonth(session);
            }

            Assert.Equal(0, session.World.Player.Job!.Warnings);
        }

        [Fact]
        public void Equipo_EnUnOficio_LaJefaturaTieneSuNombre()
        {
            LifeSession session = WorkingIn(Vestuario);
            Weeks(session, 1);
            var presenter = new LifePresenter(session);

            string tag = presenter.Team()!.Boss!.Bond!;

            Assert.Contains(tag, new[] { "Jefa de tienda", "Jefe de tienda" });
        }

        [Fact]
        public void Guardado_ConservaLaMetaLoVariableYElDesafio()
        {
            LifeSession session = WorkingIn(Vestuario);
            NextMonth(session);
            NextMonth(session);
            Run(session.World, "jugador.amonestaciones = 2");
            Run(session.World, "jugador.desafio('desafio_vendedor_encargado')");
            Job before = session.World.Player.Job!;

            LifeSession loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes());
            Job after = loaded.World.Player.Job!;

            Assert.Equal(before.GoalPercent, after.GoalPercent);
            Assert.Equal(before.MonthsUnderGoal, after.MonthsUnderGoal);
            Assert.Equal(before.MonthsOnGoal, after.MonthsOnGoal);
            Assert.Equal(2, after.Warnings);
            Assert.Equal(before.VariablePay, after.VariablePay);
            Assert.Equal("desafio_vendedor_encargado", after.Challenge);
        }

        [Fact]
        public void Aviso_QuienBuscaSinTitulo_PuedePostularAUnOficio()
        {
            var session = new LifeSession(Content, 9, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (20 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, "jugador.desmarcar('trabajando')");
            Run(world, "jugador.marcar('buscando_trabajo')");
            Run(world, "jugador.dejar_de_buscar()");
            Run(world, "jugador.marcar('buscando_trabajo')");

            var definition = Content.Events.First(e => e.Id == "evento.garzon.aviso");
            Assert.True(definition.CompiledConditions!.IsTrue(Scope(world)));
            Run(world, $"jugador.postular_en('{Restoran}')");
            Assert.Equal(Restoran, Eval(world, "jugador.busca_en").AsText("prueba"));
        }
    }
}

using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5a4: talleres, electivos y seminario de tesis de la carrera, con proyecto y entregas.</summary>
    public class CareerProjectTests
    {
        private const string Civil = "carrera.software.nivel.ingenieria_civil_computacion";
        private const string Workshop = "ramo.software.taller_desarrollo";
        private const string Elective = "ramo.software.electivo";

        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, Content, session.World.Player, null, readOnly: false));

        private static Value Eval(LifeSession session, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(session.World, Content, session.World.Player, null, readOnly: true));

        private static WeekCycle University() =>
            new WeekCycle().Register(new CareerSystem()).Register(new UniversitySystem(Content));

        /// <summary>Una vida que estudia Ingeniería Civil en Computación y empieza el 9º semestre, con su sección.</summary>
        private static LifeSession NinthSemester(ulong seed = 3)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, (22 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(session, $"estudiar('{Civil}')");
            world.Player.Studies!.CompletedSemesters = 8;
            world.Player.Studies.GradedSemesters = 8;
            University().AdvanceWeeks(world, 1, out _);
            return session;
        }

        [Fact]
        public void Taller_LasEntregasTocanRepartidasEnElSemestre()
        {
            LifeSession session = NinthSemester();
            CareerStudies studies = session.World.Player.Studies!;
            CourseDefinition workshop = Content.GetCareer("carrera.software").Courses.Single(c => c.Id == Workshop);
            Assert.Equal(3, workshop.Deliveries);
            Assert.True(Eval(session, $"jugador.cursa('{Workshop}')").AsBool("cursa"));
            Assert.Equal(string.Empty, Eval(session, "jugador.entrega_pendiente").AsText("entrega"));

            University().AdvanceWeeks(session.World, ProjectRules.DueWeek(workshop, 1) - studies.WeeksIntoSemester, out _);

            Assert.Equal(Workshop, Eval(session, "jugador.entrega_pendiente").AsText("entrega"));
            Assert.Equal(1, Eval(session, "jugador.numero_entrega").AsNumber("numero"));
            Run(session, "jugador.entregar(1)");
            Assert.Single(studies.ProjectDeliveries[Workshop]);
            Assert.NotEqual(Workshop, Eval(session, "jugador.entrega_pendiente").AsText("entrega"));
        }

        [Fact]
        public void Entrega_MasEsfuerzoDaMasCalidad_YLaNotaSaleDeLasEntregas()
        {
            LifeSession session = NinthSemester();
            Character player = session.World.Player;
            Assert.True(ProjectRules.Quality(session.World, player, 2) > ProjectRules.Quality(session.World, player, -1));

            CourseDefinition workshop = Content.GetCareer("carrera.software").Courses.Single(c => c.Id == Workshop);
            GradeScaleDefinition scale = Content.GetCountry("pais.chile").GradeScale!;
            player.Studies!.ProjectDeliveries[Workshop] = new System.Collections.Generic.List<int> { 100, 100, 100 };
            Assert.Equal(scale.Max, ProjectRules.Mark(session.World, player, workshop, scale));
            player.Studies.ProjectDeliveries[Workshop] = new System.Collections.Generic.List<int> { 50, 50, 50 };
            Assert.Equal(scale.Pass, ProjectRules.Mark(session.World, player, workshop, scale));
            player.Studies.ProjectDeliveries[Workshop] = new System.Collections.Generic.List<int> { 0, 0, 0 };
            Assert.Equal(scale.Min, ProjectRules.Mark(session.World, player, workshop, scale));
        }

        [Fact]
        public void Semestre_AlCerrar_PoneLaNotaDelTallerYVaciaLasEntregas()
        {
            LifeSession session = NinthSemester();
            Character player = session.World.Player;
            player.Studies!.ProjectDeliveries[Workshop] = new System.Collections.Generic.List<int> { 100, 100, 100 };

            University().AdvanceWeeks(session.World, CareerStudies.WeeksPerSemester, out _);

            Assert.Equal(7.0, player.Studies.LastMarks[Workshop]);
            Assert.Empty(player.Studies.ProjectDeliveries);
        }

        [Fact]
        public void Electivo_MarcaTuArea_YTeLlamanMasAEntrevistasDeEsaArea()
        {
            LifeSession session = NinthSemester();
            Character player = session.World.Player;
            Assert.True(Eval(session, $"jugador.cursa('{Elective}')").AsBool("cursa"));

            Run(session, "jugador.elegir_electivo('carrera.software.area.datos')");
            Run(session, "jugador.elegir_electivo('carrera.software.area.qa')");

            Assert.Equal("carrera.software.area.datos", Eval(session, "jugador.electivo").AsText("electivo"));
            Assert.Throws<ScriptException>(() => Run(session, "jugador.elegir_electivo('carrera.psicologia.area.clinica')"));

            var datos = new JobSearch("carrera.software.area.datos") { MethodId = "busqueda.forma.portales" };
            var qa = new JobSearch("carrera.software.area.qa") { MethodId = "busqueda.forma.portales" };
            double withElective = JobSearchRules.InterviewChance(session.World, Content, player, datos);
            double without = JobSearchRules.InterviewChance(session.World, Content, player, qa);
            Assert.Equal(without * 1.25, withElective, 6);
        }

        [Fact]
        public void Guardado_ConservaLasEntregasDelProyecto()
        {
            LifeSession session = NinthSemester();
            session.World.Player.Studies!.ProjectDeliveries[Workshop] = new System.Collections.Generic.List<int> { 61, 72 };

            Character after = LifeSession.LoadFromBytes(Content, session.SaveToBytes()).World.Player;

            Assert.Equal(new[] { 61, 72 }, after.Studies!.ProjectDeliveries[Workshop]);
        }
    }
}

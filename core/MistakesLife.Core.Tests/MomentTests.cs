using System.Linq;
using MistakesLife.Core.Analysis;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Momentos del rediseño: pasan con alguien o en un lugar con nombre, y nacen de una
    /// fecha (un cumpleaños, Navidad) o de tu horario (la plaza, la once con tu mamá).
    /// </summary>
    public class MomentTests
    {
        private const string Activity = @"[ { ""id"": ""actividad.plaza"" } ]";

        private static ContentDatabase Content(string events, string activities = Activity)
        {
            var content = new ContentDatabase();
            content.AddActivitiesJson("pack.prueba", "actividades/a.json", activities);
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        private static WeekCycle Events(ContentDatabase content) =>
            new WeekCycle().Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 0 }));

        private static Character Mother(World world) => ChatSystem.PeopleAs(world, world.Player, "madre", minAge: 0).Single();

        [Fact]
        public void Momento_ConFecha_PasaLaSemanaDeEseDiaUnaVezPorAno()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.nochebuena"", ""fecha"": ""24-12"", ""lugar"": ""lugar.casa"" } ]");
            World world = WorldFactory.CreateNewLife(3, RepoPacks.Content, "pais.chile");

            Events(content).AdvanceWeeks(world, 3 * GameDate.WeeksPerYear);

            Assert.Equal(3, world.EventHistory.Count);
            Assert.All(world.EventHistory, r => Assert.Equal((12, 24), (r.Date.Month, r.Date.DayOfMonth)));
        }

        [Fact]
        public void Momento_DeCumpleanos_PasaLaSemanaDelCumpleanosDeEsaPersona()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.cumple_mama"", ""fecha"": ""cumpleanos"", ""con"": ""madre"", ""lugar"": ""lugar.casa"" } ]");
            World world = WorldFactory.CreateNewLife(3, RepoPacks.Content, "pais.chile");
            Character mother = Mother(world);

            Events(content).AdvanceWeeks(world, 2 * GameDate.WeeksPerYear);

            Assert.Equal(2, world.EventHistory.Count);
            Assert.All(world.EventHistory, r => Assert.True(r.Date.SameWeekOfYear(mother.BirthDate)));
        }

        [Fact]
        public void Momento_ConPersona_SeAcuerdaDeQuienEsYSusEfectosLeLlegan()
        {
            ContentDatabase content = Content(@"[ {
              ""id"": ""evento.once"", ""con"": ""madre"", ""probabilidad_anual"": 1,
              ""opciones"": [ { ""texto"": ""opcion.escuchar"", ""efectos"": [ ""persona.carino += 10"" ] } ] } ]");
            World world = WorldFactory.CreateNewLife(3, RepoPacks.Content, "pais.chile");
            Character mother = Mother(world);
            int before = RelationshipRules.Feelings(world, mother, world.Player).Affection;

            Events(content).AdvanceWeeks(world, GameDate.WeeksPerYear, out bool stopped);
            PendingEvent pending = Assert.Single(world.PendingEvents);
            new EventSystem(content).Choose(world, pending, 0);

            Assert.True(stopped);
            Assert.Equal(mother.Id, pending.PersonId);
            Assert.Equal(before + 10, RelationshipRules.Feelings(world, mother, world.Player).Affection);
        }

        [Fact]
        public void Momento_ConPersona_SinNadieAsi_NoPasa()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.con_pareja"", ""con"": ""pareja"", ""probabilidad_anual"": 1 } ]");
            World world = WorldFactory.CreateNewLife(3, RepoPacks.Content, "pais.chile");

            Events(content).AdvanceWeeks(world, GameDate.WeeksPerYear);

            Assert.Empty(world.EventHistory);
        }

        [Fact]
        public void Momento_DelHorario_SoloPasaSiLaActividadEstaEnTuSemana()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.en_la_plaza"", ""actividad"": ""actividad.plaza"", ""lugar"": ""lugar.plaza"", ""probabilidad_anual"": 1, ""unico"": true } ]");
            World world = WorldFactory.CreateNewLife(3, RepoPacks.Content, "pais.chile");
            WeekCycle cycle = Events(content);

            cycle.AdvanceWeeks(world, GameDate.WeeksPerYear);
            Assert.Empty(world.EventHistory);

            var routine = new Routine("prueba");
            routine.Set(2, RoutineBlock.Afternoon, "actividad.plaza");
            world.Player.Routine = routine;
            cycle.AdvanceWeeks(world, GameDate.WeeksPerYear);

            Assert.Single(world.EventHistory, r => r.EventId == "evento.en_la_plaza");
        }

        [Theory]
        [InlineData(@"{ ""id"": ""evento.x"", ""fecha"": ""24-12"" }", "con quién (con) o dónde (lugar)")]
        [InlineData(@"{ ""id"": ""evento.x"", ""fecha"": ""30-02"", ""lugar"": ""lugar.casa"" }", "no se entiende")]
        [InlineData(@"{ ""id"": ""evento.x"", ""fecha"": ""cumpleanos"", ""lugar"": ""lugar.casa"" }", "cumpleaños de esa persona")]
        [InlineData(@"{ ""id"": ""evento.x"", ""actividad"": ""actividad.plaza"", ""lugar"": ""lugar.plaza"" }", "probabilidad_anual")]
        [InlineData(@"{ ""id"": ""evento.x"", ""actividad"": ""actividad.no_existe"", ""lugar"": ""lugar.plaza"", ""probabilidad_anual"": 0.5 }", "no existe")]
        [InlineData(@"{ ""id"": ""evento.x"", ""con"": ""vecino"" }", "con 'vecino'")]
        [InlineData(@"{ ""id"": ""evento.x"", ""lugar"": ""plaza"" }", "lugar.")]
        [InlineData(@"{ ""id"": ""evento.x"", ""efectos"": [ ""persona.carino += 1"" ] }", "'persona'")]
        public void Momento_MalEscrito_SeRechazaConUnMensajeClaro(string ev, string message)
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content("[" + ev + "]"));

            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.x") && e.Message.Contains(message));
        }

        /// <summary>Juega una vida en Chile hasta el primer momento que pasa con alguien.</summary>
        private static LifeSession UntilMomentWithPerson(ulong seed)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            var decider = new Decisions.AutoDecider(session.Content);
            var random = new Randomness.GameRandom(seed);
            while (session.PlayerAgeInYears < 18)
            {
                PendingEvent? pending = session.Pending;
                if (pending?.PersonId != null)
                {
                    return session;
                }

                if (pending != null)
                {
                    session.Choose(decider.Choose(session.World, pending, random));
                }
                else
                {
                    session.Advance(GameDate.WeeksPerYear);
                }
            }

            throw new Xunit.Sdk.XunitException("Ningún momento con alguien antes de los 18.");
        }

        [Fact]
        public void Momento_EnPantalla_MuestraConQuienYDondeYCambiaSuNombreEnElTexto()
        {
            LifeSession session = UntilMomentWithPerson(5);
            PendingEvent pending = session.Pending!;
            Character person = session.World.GetCharacter(pending.PersonId!.Value);
            EventDefinition definition = session.Content.GetEvent(pending.EventId);

            EventView view = new LifePresenter(session).Snapshot().PendingEvent!;

            string expected = definition.With == "madre" ? "tu mamá" : definition.With == "padre" ? "tu papá" : person.FirstName;
            Assert.Equal(expected, view.Person);
            Assert.Equal("Con " + expected + " · " + session.Text(definition.Place!), new LifePresenter(session).MomentLine(view));
            Assert.Equal(person.Id, view.PersonId);
            Assert.Equal(session.Text(definition.Place!), view.Place);
            Assert.DoesNotContain(LifePresenter.PersonMark, view.Text);
            Assert.All(view.Options, o => Assert.DoesNotContain(LifePresenter.PersonMark, o));
        }

        [Fact]
        public void Momento_PendienteConPersona_SeGuardaYSeCarga()
        {
            LifeSession session = UntilMomentWithPerson(6);
            int person = session.Pending!.PersonId!.Value;

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.Equal(person, loaded.Pending!.PersonId);
        }

        [Fact]
        public void Simulador_CuentaLasVecesQueSeDetieneElTiempoPorAnoSegunLaEdad()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(3, 8, "pais.chile");

            Assert.True(report.StopsPerYear[0] > 0);
            Assert.True(report.StopsPerYear[1] > 0);
            Assert.Null(report.StopsPerYear[2]);
            Assert.Contains("Veces que se detiene el tiempo por año", report.ToMarkdown());
        }
    }
}

using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class CareerTests
    {
        private const string CareerJson = @"{
          ""id"": ""carrera.prueba"",
          ""niveles"": [
            { ""id"": ""carrera.prueba.nivel.tecnico"", ""institucion"": ""cft"", ""semestres"": 4 },
            { ""id"": ""carrera.prueba.nivel.profesional"", ""institucion"": ""universidad"", ""semestres"": 10,
              ""condiciones"": ""jugador.tiene('puede_universidad')"" }
          ]
        }";

        private const string EventsJson = @"[
          { ""id"": ""evento.elegir"", ""peso"": 1000, ""unico"": true, ""opciones_carreras"": true,
            ""opciones"": [ { ""texto"": ""opcion.trabajar"", ""efectos"": [""jugador.marcar('trabaja')""] } ] },
          { ""id"": ""evento.titulacion"", ""peso"": 1000,
            ""condiciones"": ""jugador.estudia == 'carrera.prueba' && jugador.semestres_aprobados >= jugador.semestres_carrera"",
            ""efectos"": [""jugador.titularse()""] }
        ]";

        private static ContentDatabase Content(string careerJson = CareerJson, string eventsJson = EventsJson)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson);
            content.AddCareerJson("pack.prueba", "career.json", careerJson);
            content.AddEventsJson("pack.prueba", "events/prueba.json", eventsJson);
            return content.Build();
        }

        [Fact]
        public void EventoConOpcionesCarreras_AgregaUnaOpcionPorNivelAntesDeLasPropias()
        {
            EventDefinition ev = Content().GetEvent("evento.elegir");

            Assert.Equal(
                new[] { "carrera.prueba.nivel.tecnico", "carrera.prueba.nivel.profesional", "opcion.trabajar" },
                ev.Options.Select(o => o.TextKey).ToArray());
        }

        [Fact]
        public void NivelConCondiciones_SoloApareceSiSeCumplen()
        {
            var session = new LifeSession(Content(), 5, new Seeds.SeedRules(), new EventRules { WeeklyRandomEventChance = 1.0 });
            session.Advance(1);

            PendingEvent pending = Assert.IsType<PendingEvent>(session.Pending);
            Assert.Equal(new[] { 0, 2 }, pending.AvailableOptions.ToArray()); // sin 'puede_universidad', no hay universidad
        }

        [Fact]
        public void Estudiar_AvanzaUnSemestreCada26SemanasYTermina_ConTitulo()
        {
            var session = new LifeSession(Content(), 5, new Seeds.SeedRules(), new EventRules { WeeklyRandomEventChance = 1.0 });
            session.Advance(1);
            session.Choose(0); // técnico, 4 semestres

            Character player = session.World.Player;
            Assert.NotNull(player.Studies);
            Assert.Equal("cft", player.Studies!.Institution);

            session.Advance(CareerStudies.WeeksPerSemester * 2);
            Assert.Equal(2, player.Studies.CompletedSemesters);

            session.Advance(CareerStudies.WeeksPerSemester * 3);
            Assert.Null(player.Studies);
            Assert.Equal(new[] { "carrera.prueba.nivel.tecnico" }, player.Degrees.ToArray());
        }

        [Fact]
        public void Lenguaje_TituladoEnYDesertar_Funcionan()
        {
            var world = WorldFactory.CreateNewLife(1);
            var content = Content();
            var scope = new EventScope(world, content, world.Player, null, readOnly: false);
            Scripting.Effect.Parse("estudiar('carrera.prueba.nivel.tecnico')").Apply(scope);
            Assert.True(Scripting.Expression.Parse("jugador.estudia == 'carrera.prueba' && jugador.institucion == 'cft'").IsTrue(scope));

            Scripting.Effect.Parse("jugador.semestres_aprobados = 4").Apply(scope);
            Scripting.Effect.Parse("jugador.titularse()").Apply(scope);
            Assert.True(Scripting.Expression.Parse("jugador.titulado && jugador.titulado_en('carrera.prueba') && jugador.estudia == nada").IsTrue(scope));
            Assert.False(Scripting.Expression.Parse("jugador.titulado_en('carrera.otra')").IsTrue(scope));

            Scripting.Effect.Parse("estudiar('carrera.prueba.nivel.profesional')").Apply(scope);
            Scripting.Effect.Parse("jugador.desertar()").Apply(scope);
            Assert.Null(world.Player.Studies);
            Assert.Single(world.Player.Degrees);
        }

        [Fact]
        public void CarreraMalEscrita_SeRechazaConErroresClaros()
        {
            const string bad = @"{ ""id"": ""carrera.mala"", ""niveles"": [
              { ""id"": ""otra.cosa"", ""institucion"": ""colegio"", ""semestres"": 0 } ] }";
            const string events = @"[ { ""id"": ""evento.x"", ""efectos"": [""estudiar('carrera.no_existe.nivel.x')""] } ]";

            var ex = Assert.Throws<ContentLoadException>(() => Content(bad, events));

            Assert.Contains(ex.Errors, e => e.Message.Contains("carrera.mala.nivel."));
            Assert.Contains(ex.Errors, e => e.Message.Contains("institucion"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("semestres"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("ese nivel de carrera no existe"));
        }

        [Fact]
        public void PaquetesReales_ChileOfreceLasCarrerasAlTerminarElLiceo()
        {
            EventDefinition ev = RepoPacks.Content.GetEvent("evento.elegir_carrera");

            Assert.Contains(ev.Options, o => o.TextKey == "carrera.software.nivel.ingenieria_civil_computacion");
            Assert.Equal("opcion.carrera.mejor_trabajar", ev.Options.Last().TextKey);
        }
    }
}

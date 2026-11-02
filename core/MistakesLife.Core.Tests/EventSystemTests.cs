using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class EventSystemTests
    {
        // Semillas y eventos del ejemplo del documento técnico (copiar en la PAES).
        private const string SeedsJson = @"[
          { ""id"": ""semilla.copio_paes"", ""tipo"": ""negativa"", ""fuerza"": 40, ""vence_anios"": 20,
            ""condiciones"": ""jugador.tiene('postulando')"" },
          { ""id"": ""semilla.mentira_entrevista"", ""tipo"": ""negativa"", ""fuerza"": 30, ""vence_anios"": 5 }
        ]";

        private const string EventsJson = @"[
          { ""id"": ""evento.prueba_paes"", ""condiciones"": ""jugador.edad >= 17 && !jugador.tiene('rindio_paes')"",
            ""peso"": 1000, ""dia_clave"": true, ""unico"": true,
            ""efectos"": [""jugador.marcar('rindio_paes')""],
            ""opciones"": [
              { ""texto"": ""opcion.copiar"", ""efectos"": [""plantar('semilla.copio_paes')"", ""diario('diario.copie_paes')""] },
              { ""texto"": ""opcion.no_copiar"", ""efectos"": [""jugador.felicidad -= 5""] }
            ] },
          { ""id"": ""evento.entrevista_testigo_copia"", ""semilla"": ""semilla.copio_paes"", ""peso"": 40, ""dia_clave"": true,
            ""opciones"": [
              { ""texto"": ""opcion.reconocer_error"", ""efectos"": [""jugador.felicidad += 10"", ""semilla('semilla.copio_paes').cerrar""] },
              { ""texto"": ""opcion.negar"", ""efectos"": [""plantar('semilla.mentira_entrevista')""] },
              { ""texto"": ""opcion.solo_si_muy_sano"", ""condiciones"": ""jugador.salud > 200"" }
            ] },
          { ""id"": ""evento.mentira_descubierta"", ""semilla"": ""semilla.mentira_entrevista"" }
        ]";

        /// <summary>Semanas hasta simular la primera semana con 17 años cumplidos.</summary>
        private const int Age17 = (17 * GameDate.WeeksPerYear) + 1;

        private static ContentDatabase Content(string seeds = SeedsJson, string events = EventsJson)
        {
            var content = new ContentDatabase();
            content.AddSeedsJson("pack.prueba", "seeds/prueba.json", seeds);
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        private static (World World, WeekCycle Cycle, EventSystem Events) Setup(ContentDatabase content, double randomChance = 1.0)
        {
            var world = WorldFactory.CreateNewLife(7);
            var events = new EventSystem(content, new EventRules { WeeklyRandomEventChance = randomChance });
            var cycle = new WeekCycle()
                .Register(new SeedSystem(new SeedRules { WeeklyWakeChanceAtFullStrength = 100.0 }))
                .Register(events);
            return (world, cycle, events);
        }

        [Fact]
        public void EventoAlAzar_RespetaSusCondiciones()
        {
            var (world, cycle, _) = Setup(Content());

            // La semana que empieza justo al cumplir 17 años es la primera posible.
            cycle.AdvanceWeeks(world, 17 * GameDate.WeeksPerYear);
            Assert.Empty(world.PendingEvents);

            int weeks = cycle.AdvanceWeeks(world, 10);
            Assert.Equal(1, weeks); // día clave: el tiempo se detuvo
            PendingEvent pending = Assert.Single(world.PendingEvents);
            Assert.Equal("evento.prueba_paes", pending.EventId);
            Assert.True(world.Player.HasFlag("rindio_paes")); // efectos al ocurrir
        }

        [Fact]
        public void ElegirUnaOpcion_AplicaSusEfectosYLaAnota()
        {
            var (world, cycle, events) = Setup(Content());
            cycle.AdvanceWeeks(world, Age17);
            PendingEvent pending = world.PendingEvents.Single();

            events.Choose(world, pending, 0);

            Assert.Empty(world.PendingEvents);
            Seed seed = Assert.Single(world.Seeds.OpenOfType("semilla.copio_paes"));
            Assert.Equal(40, seed.Strength);
            Assert.Equal(17, seed.OwnerAgeAtPlanting);
            Assert.Contains(world.Journal, e => e.TextKey == "diario.copie_paes");
            EventRecord record = world.EventHistory.Single();
            Assert.Equal(0, record.ChosenOption);
        }

        [Fact]
        public void EventoUnico_NoSeRepite()
        {
            var (world, cycle, events) = Setup(Content());
            cycle.AdvanceWeeks(world, Age17);
            events.Choose(world, world.PendingEvents.Single(), 1);
            world.Player.ClearFlag("rindio_paes");

            cycle.AdvanceWeeks(world, 100);

            Assert.Single(world.EventHistory, r => r.EventId == "evento.prueba_paes");
        }

        [Fact]
        public void EventoPendiente_FrenaNuevosEventos()
        {
            var (world, cycle, _) = Setup(Content(events: @"[
              { ""id"": ""evento.a"", ""opciones"": [ { ""texto"": ""opcion.ok"" } ] }
            ]", seeds: "[]"));

            cycle.AdvanceWeeks(world, 20);

            Assert.Single(world.PendingEvents);
        }

        [Fact]
        public void EventoSinOpciones_SeResuelveSolo()
        {
            var (world, cycle, _) = Setup(Content(events: @"[
              { ""id"": ""evento.resfrio"", ""efectos"": [""jugador.salud -= 1""] }
            ]", seeds: "[]"));
            int before = world.Player.Stats.Health;

            cycle.AdvanceWeeks(world, 3);

            Assert.Empty(world.PendingEvents);
            Assert.Equal(3, world.EventHistory.Count);
            Assert.Equal(Math.Max(0, before - 3), world.Player.Stats.Health);
        }

        [Fact]
        public void OpcionConCondicionFalsa_NoSeOfrece()
        {
            var (world, cycle, _) = Setup(Content());
            world.Seeds.Plant(Content().GetSeedType("semilla.copio_paes").ToSeedDefinition(Content()), world.PlayerId, world.Date, 0);
            world.Player.SetFlag("postulando");
            world.Player.SetFlag("rindio_paes");

            cycle.AdvanceWeek(world);

            PendingEvent pending = world.PendingEvents.Single();
            Assert.Equal("evento.entrevista_testigo_copia", pending.EventId);
            Assert.Equal(new[] { 0, 1 }, pending.AvailableOptions.ToArray());
        }

        [Fact]
        public void EjemploDelDocumentoTecnico_CopiarEnLaPaes_VuelveEnLaEntrevista()
        {
            var content = Content();
            var (world, cycle, events) = Setup(content, randomChance: 1.0);

            // 17 años: rinde la PAES y copia.
            cycle.AdvanceWeeks(world, Age17);
            events.Choose(world, world.PendingEvents.Single(), 0);
            Seed copia = world.Seeds.OpenOfType("semilla.copio_paes").Single();

            // Años después, sin postular, la semilla duerme.
            cycle.AdvanceWeeks(world, 5 * GameDate.WeeksPerYear);
            Assert.Equal(SeedState.Dormant, copia.State);

            // Postula a un trabajo: la semilla despierta y aparece la entrevista.
            world.Player.SetFlag("postulando");
            cycle.AdvanceWeeks(world, 4);
            PendingEvent entrevista = world.PendingEvents.Single();
            Assert.Equal("evento.entrevista_testigo_copia", entrevista.EventId);
            Assert.Equal(copia.Id, entrevista.SeedId);

            // Niega: la semilla original se cierra y se planta la mentira.
            events.Choose(world, entrevista, 1);
            Assert.Equal(SeedState.Resolved, copia.State);
            Assert.Single(world.Seeds.OpenOfType("semilla.mentira_entrevista"));
        }

        [Fact]
        public void MismaPartida_MismosEventos()
        {
            string[] Run()
            {
                var (world, cycle, _) = Setup(Content(events: @"[
                  { ""id"": ""evento.a"", ""peso"": 3 }, { ""id"": ""evento.b"", ""peso"": 1 }, { ""id"": ""evento.c"", ""peso"": 6 }
                ]", seeds: "[]"), randomChance: 0.5);
                cycle.AdvanceWeeks(world, 300);
                return world.EventHistory.Select(r => r.Date.TotalWeeks + r.EventId).ToArray();
            }

            Assert.Equal(Run(), Run());
        }

        [Fact]
        public void Pesos_SeRespetanAproximadamente()
        {
            var (world, cycle, _) = Setup(Content(events: @"[
              { ""id"": ""evento.comun"", ""peso"": 9 }, { ""id"": ""evento.raro"", ""peso"": 1 }
            ]", seeds: "[]"));

            cycle.AdvanceWeeks(world, 2000);

            int raro = world.EventHistory.Count(r => r.EventId == "evento.raro");
            Assert.InRange(raro, 120, 280);
        }

        [Fact]
        public void Enfriamiento_EvitaQueUnEventoSeRepitaSeguido()
        {
            var (world, cycle, _) = Setup(Content(events: @"[
              { ""id"": ""evento.comun"", ""enfriamiento_semanas"": 10 }
            ]", seeds: "[]"));

            cycle.AdvanceWeeks(world, 100);

            int[] weeks = world.EventHistory.Select(r => r.Date.TotalWeeks).ToArray();
            Assert.Equal(10, weeks.Length);
            Assert.All(weeks.Zip(weeks.Skip(1), (a, b) => b - a), gap => Assert.True(gap >= 10));
        }

        [Fact]
        public void PaquetesDelRepositorio_CarganSinErrores()
        {
            Assert.NotEmpty(RepoPacks.Content.Events);
        }
    }
}

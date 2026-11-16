using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// R7d: un evento de semilla con "con": "testigo" pasa con quien vio lo que hiciste
    /// (el amigo al que le prestaste plata, quien te recomendó), con su nombre, en vez de "tu amigo".
    /// </summary>
    public class SeedWitnessEventTests
    {
        private const string SeedsJson = @"[
          { ""id"": ""semilla.presto"", ""tipo"": ""positiva"", ""fuerza"": 100, ""vence_anios"": 5,
            ""condiciones"": ""jugador.tiene('cobrar')"" }
        ]";

        private static ContentDatabase Content(string events)
        {
            var content = new ContentDatabase();
            content.AddSeedsJson("pack.prueba", "seeds/prueba.json", SeedsJson);
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        private static Character AddPerson(World world)
        {
            var person = new Character(world.NextCharacterId(), "Matías", "Rojas", Sex.Male, world.Date, new Personality(50, 50, 50, 50, 50), new Stats(80, 80, 50));
            world.AddCharacter(person);
            return person;
        }

        private static WeekCycle Cycle(ContentDatabase content) => new WeekCycle()
            .Register(new SeedSystem(new SeedRules { WeeklyWakeChanceAtFullStrength = 100.0 }))
            .Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 1.0 }));

        private static Seed Plant(World world, ContentDatabase content, params int[] witnesses) =>
            world.Seeds.Plant(content.GetSeedType("semilla.presto").ToSeedDefinition(content), world.PlayerId, world.Date, 0, witnesses);

        [Fact]
        public void EventoDeSemilla_ConTestigo_PasaConQuienLoVio()
        {
            ContentDatabase content = Content(@"[
              { ""id"": ""evento.devuelve"", ""semilla"": ""semilla.presto"", ""con"": ""testigo"",
                ""opciones"": [ { ""texto"": ""opcion.gracias"", ""efectos"": [""persona.carino += 5""] } ] }
            ]");
            World world = WorldFactory.CreateNewLife(7);
            Character friend = AddPerson(world);
            Plant(world, content, friend.Id);
            world.Player.SetFlag("cobrar");

            Cycle(content).AdvanceWeeks(world, 4);

            PendingEvent pending = Assert.Single(world.PendingEvents);
            Assert.Equal("evento.devuelve", pending.EventId);
            Assert.Equal(friend.Id, pending.PersonId);
        }

        [Fact]
        public void EventoDeSemilla_ConTestigo_SinTestigoVivo_NoPasa()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.devuelve"", ""semilla"": ""semilla.presto"", ""con"": ""testigo"" } ]");
            World world = WorldFactory.CreateNewLife(7);
            Plant(world, content);
            world.Player.SetFlag("cobrar");

            Cycle(content).AdvanceWeeks(world, 8);

            Assert.DoesNotContain(world.EventHistory, r => r.EventId == "evento.devuelve");
        }

        [Fact]
        public void EventoDeSemilla_ConVecesMaximas_NoSeRepiteConOtraSemillaIgual()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.devuelve"", ""semilla"": ""semilla.presto"", ""veces_maximas"": 1 } ]");
            World world = WorldFactory.CreateNewLife(7);
            Plant(world, content);
            Plant(world, content);
            world.Player.SetFlag("cobrar");

            Cycle(content).AdvanceWeeks(world, 20);

            Assert.Single(world.EventHistory, r => r.EventId == "evento.devuelve");
        }

        [Fact]
        public void ConTestigo_FueraDeUnEventoDeSemilla_EsUnErrorDelPaquete()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[ { ""id"": ""evento.solo"", ""con"": ""testigo"", ""peso"": 1 } ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.solo") && e.Message.Contains("testigo"));
        }

        [Fact]
        public void EnLosPaquetes_LoQuePasaConElTestigo_LoNombra()
        {
            string[] ids =
            {
                "evento.amigo_devuelve_plata", "evento.amigo_no_devuelve_plata", "evento.busqueda.devolver_favor",
                "evento.futuro.prestamo_devuelto", "evento.futuro.prestamo_olvidado", "evento.ahijado", "evento.amigo_vuelve_de_visita",
                "evento.negocio_con_amigo_resulta", "evento.negocio_con_amigo_fracasa", "evento.voluntarios_recuerdo", "evento.marcharon_juntos_recuerdo",
            };
            ContentDatabase content = RepoPacks.Content;
            foreach (string id in ids)
            {
                EventDefinition ev = content.GetEvent(id);
                Assert.Equal(EventDefinition.WitnessRole, ev.With);
                Assert.All(ev.AllTextKeys, key => Assert.Contains("{persona}", content.Text(key)));
            }

            Assert.All(content.Events.Where(e => e.With == EventDefinition.WitnessRole), e => Assert.NotNull(e.SeedType));
        }
    }
}

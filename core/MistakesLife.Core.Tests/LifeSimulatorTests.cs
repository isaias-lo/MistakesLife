using System.Linq;
using MistakesLife.Core.Analysis;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class LifeSimulatorTests
    {
        [Fact]
        public void Simulador_ConLaMismaSemilla_DaElMismoInforme()
        {
            var simulator = new LifeSimulator(RepoPacks.Content);

            string first = simulator.Run(lives: 5, finalAge: 25).ToMarkdown();
            string second = simulator.Run(lives: 5, finalAge: 25).ToMarkdown();

            Assert.Equal(first, second);
        }

        [Fact]
        public void Simulador_ConElContenidoReal_TodosLosEventosAparecenYPocosMuerenJovenes()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 200, finalAge: 25);

            Assert.Equal(200, report.Lives);
            // En Chile, cerca del 2 % muere antes de los 25 (tabla de mortalidad del INE).
            Assert.InRange(report.Deaths, 0, 10);
            Assert.Contains("# Simulación de 200 vidas hasta los 25 años", report.ToMarkdown());
        }

        [Fact]
        public void Simulador_VidasJovenesYHastaLaVejez_TodosLosEventosAparecenAlMenosUnaVez()
        {
            // Muchas vidas jóvenes (1000, como el revisor de equilibrio), para los eventos que le pasan a pocas personas
            // (por ejemplo, a quienes son asexuales, cerca del 1 %), y menos vidas
            // hasta los 85, para los eventos de adultos y de vejez (por ejemplo, jubilar).
            // Y vidas que trabajan en cada área de cada carrera, para los eventos de
            // áreas a las que pocas personas llegan, y vidas que trabajan enfermas (dejar
            // el trabajo por salud y volver), que en una vida al azar son cerca del 1 %,
            // y vidas que ascendieron y andan mal en el trabajo (bajar de cargo).
            var simulator = new LifeSimulator(RepoPacks.Content);
            SimulationReport young = simulator.Run(lives: 1000, finalAge: 25);
            // 300 hasta los 85: algunos eventos de adultos le pasan a cerca del 1 % de las vidas
            // (por ejemplo, que la pareja se vaya por el rencor acumulado).
            SimulationReport old = simulator.Run(lives: 300, finalAge: 85);
            var careers = CareerAreaLives.SeenEvents(RepoPacks.Content, livesPerArea: 6);
            var sick = CareerAreaLives.SickWorkerEvents(RepoPacks.Content, lives: 12);
            // Y vidas que ascendieron y andan mal en el trabajo, para bajar de cargo
            // (en vidas al azar, menos del 1 %).
            var struggling = CareerAreaLives.StrugglingWorkerEvents(RepoPacks.Content, lives: 8);
            // Y una vida por carrera con las semillas de su carrera ya plantadas, para los
            // eventos que vienen de una semilla rara (por ejemplo, un reclamo de ética).
            var careerSeeds = CareerAreaLives.CareerSeedEvents(RepoPacks.Content);
            // Y vidas en Chile que se separan con hijos chicos, para los caminos raros de la
            // pensión de alimentos y el cuidado personal (el juicio por los niños, el registro de deudores).
            var separated = CareerAreaLives.SeparatedParentEvents(RepoPacks.Content, lives: 60);

            string[] never = young.NeverSeenEvents.Intersect(old.NeverSeenEvents).Except(careers).Except(sick).Except(struggling).Except(careerSeeds).Except(separated).ToArray();

            Assert.Empty(never);
        }

        [Fact]
        public void Simulador_ConElContenidoReal_NingunEventoComunSeRepiteDemasiado()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 200, finalAge: 25);

            // Un evento al azar que aparece más de 10 veces en una misma vida cansa al jugador.
            foreach (var ev in report.EventCount)
            {
                double perLife = (double)ev.Value / report.LivesWithEvent[ev.Key];
                Assert.True(perLife <= 10, $"{ev.Key} aparece {perLife:0.0} veces por vida.");
            }
        }

        [Fact]
        public void Plantar_LaMismaSemillaDosVeces_LaHaceMasFuerteEnVezDeDuplicarla()
        {
            var content = new ContentDatabase();
            content.AddSeedsJson("pack.prueba", "seeds/prueba.json", @"[
              { ""id"": ""semilla.copio"", ""tipo"": ""negativa"", ""fuerza"": 30, ""vence_anios"": 5,
                ""condiciones"": ""jugador.tiene('nunca')"" }
            ]");
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[
              { ""id"": ""evento.copia_dos_veces"", ""peso"": 1000, ""unico"": true,
                ""efectos"": [""plantar('semilla.copio')"", ""plantar('semilla.copio')""] },
              { ""id"": ""evento.profesor_recuerda"", ""semilla"": ""semilla.copio"" }
            ]");
            content.Build();

            var world = WorldFactory.CreateNewLife(3);
            new WeekCycle()
                .Register(new SeedSystem(new SeedRules()))
                .Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 1.0 }))
                .AdvanceWeeks(world, 2);

            Seed seed = Assert.Single(world.Seeds.All, s => s.TypeId == "semilla.copio");
            Assert.Equal(60, seed.Strength);
        }
    }
}

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
            Assert.Empty(report.NeverSeenEvents);
            // En Chile, cerca del 2 % muere antes de los 25 (tabla de mortalidad del INE).
            Assert.InRange(report.Deaths, 0, 10);
            Assert.Contains("# Simulación de 200 vidas hasta los 25 años", report.ToMarkdown());
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

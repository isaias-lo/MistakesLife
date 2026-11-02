using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Pruebas del contenido real: Chile, Puente Alto y la carrera de software.
    /// Juegan vidas completas de 0 a 25 años eligiendo opciones al azar.
    /// </summary>
    public class ChilePackTests
    {
        private static World LiveTo25(ulong seed)
        {
            ContentDatabase content = RepoPacks.Content;
            World world = WorldFactory.CreateNewLife(seed, content);
            var events = new EventSystem(content);
            WeekCycle cycle = new WeekCycle()
                .Register(new EducationSystem(content))
                .Register(new SeedSystem())
                .Register(events);
            var chooser = new GameRandom(seed ^ 0xC0FFEE);

            int target = 25 * GameDate.WeeksPerYear;
            while (world.Player.AgeInWeeks(world.Date) < target)
            {
                foreach (PendingEvent pending in world.PendingEvents.ToList())
                {
                    events.Choose(world, pending, chooser.Pick(pending.AvailableOptions));
                }

                cycle.AdvanceWeeks(world, target - world.Player.AgeInWeeks(world.Date));
            }

            return world;
        }

        [Fact]
        public void Chile_NaceEnPuenteAlto()
        {
            World world = WorldFactory.CreateNewLife(1, RepoPacks.Content);
            Assert.Equal("comuna.puente_alto", world.Player.Residence!.CommuneId);
            Assert.Equal("Puente Alto", RepoPacks.Content.Text("comuna.puente_alto"));
        }

        [Fact]
        public void VidaCompleta_DeCeroAVeinticincoAnios_SinErrores()
        {
            World world = LiveTo25(2026);

            Assert.Equal(25, world.Player.AgeInYears(world.Date));
            string[] lived = world.EventHistory.Select(r => r.EventId).ToArray();
            Assert.Contains("evento.primer_dia_colegio", lived);
            Assert.Contains("evento.elegir_liceo", lived);
            Assert.Contains("evento.elegir_futuro", lived);
            Assert.Contains(world.Journal, e => e.TextKey == "diario.etapa.chile.media");
        }

        [Fact]
        public void CienVidas_TomanCaminosDistintos()
        {
            var paths = new Dictionary<string, int>();
            int seedsAwakened = 0;
            for (ulong seed = 0; seed < 100; seed++)
            {
                World world = LiveTo25(seed);
                string path = world.Player.HasFlag("trabaja_software") ? "desarrollador"
                    : world.Player.Studies != null ? "estudiando"
                    : world.Player.Degrees.Count > 0 ? "titulado"
                    : world.Player.HasFlag("trabajando") ? "trabajando"
                    : "otro";
                paths[path] = paths.TryGetValue(path, out int n) ? n + 1 : 1;
                seedsAwakened += world.Seeds.All.Count(s => s.AwakenedDate.HasValue);
            }

            // Ningún camino se lleva todas las vidas, y las semillas sí despiertan.
            Assert.True(paths.Count >= 2, string.Join(", ", paths.Select(p => p.Key + "=" + p.Value)));
            Assert.True(paths.Values.Max() < 90, string.Join(", ", paths.Select(p => p.Key + "=" + p.Value)));
            Assert.True(seedsAwakened > 50, $"Semillas despertadas: {seedsAwakened}");
        }

        [Fact]
        public void TodosLosTextosUsados_ExistenEnEspanol()
        {
            ContentDatabase content = RepoPacks.Content;
            foreach (var used in content.UsedTextKeys())
            {
                Assert.NotEqual(used.Key, content.Text(used.Key));
            }
        }
    }
}

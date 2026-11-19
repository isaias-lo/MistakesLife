using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public void Simulador_VidasAlMismoTiempo_DaElMismoInformeQueDeAUna()
        {
            // 9 vidas con 2 a la vez: así se juegan en más de una tanda (de 8) y la última queda a medias.
            var oneByOne = new LifeSimulator(RepoPacks.Content) { Parallelism = 1 };
            var together = new LifeSimulator(RepoPacks.Content) { Parallelism = 2 };

            string first = oneByOne.Run(lives: 9, finalAge: 30).ToMarkdown();
            string second = together.Run(lives: 9, finalAge: 30).ToMarkdown();

            Assert.Equal(first, second);
        }

        [Fact]
        [Trait(TestKinds.Name, TestKinds.ManyLives)]
        public void Simulador_ConElContenidoReal_TodosLosEventosAparecenYPocosMuerenJovenes()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 200, finalAge: 25);

            Assert.Equal(200, report.Lives);
            // En Chile, cerca del 2 % muere antes de los 25 (tabla de mortalidad del INE).
            Assert.InRange(report.Deaths, 0, 10);
            Assert.Contains("# Simulación de 200 vidas hasta los 25 años", report.ToMarkdown());
        }

        [Fact]
        [Trait(TestKinds.Name, TestKinds.ManyLives)]
        public void Simulador_VidasJovenesYHastaLaVejez_TodosLosEventosAparecenAlMenosUnaVez()
        {
            // Muchas vidas jóvenes (1000, como el revisor de equilibrio), para los eventos que le pasan a pocas personas
            // (por ejemplo, a quienes son asexuales, cerca del 1 %), y menos vidas
            // hasta los 85, para los eventos de adultos y de vejez (por ejemplo, jubilar).
            // Y vidas que trabajan en cada área de cada carrera, para los eventos de
            // áreas a las que pocas personas llegan, y vidas que trabajan enfermas (dejar
            // el trabajo por salud y volver), que en una vida al azar son cerca del 1 %,
            // y vidas que ascendieron y andan mal en el trabajo (bajar de cargo).
            // Cada grupo de vidas es independiente: se juegan todos a la vez (y el simulador,
            // además, juega sus vidas de a varias). El resultado es el mismo que de a uno.
            var simulator = new LifeSimulator(RepoPacks.Content);
            SimulationReport young = null!, old = null!;
            HashSet<string> careers = null!, sick = null!, struggling = null!, careerSeeds = null!, separated = null!,
                inheritance = null!, top = null!, freelance = null!, roommates = null!, drift = null!, partner = null!;
            Parallel.Invoke(
                () => young = simulator.Run(lives: 1000, finalAge: 25),
                // 300 hasta los 85: algunos eventos de adultos le pasan a cerca del 1 % de las vidas
                // (por ejemplo, que la pareja se vaya por el rencor acumulado).
                () => old = simulator.Run(lives: 300, finalAge: 85),
                () => careers = CareerAreaLives.SeenEvents(RepoPacks.Content, livesPerArea: 6),
                () => sick = CareerAreaLives.SickWorkerEvents(RepoPacks.Content, lives: 12),
                // Y vidas que ascendieron y andan mal en el trabajo, para bajar de cargo
                // (en vidas al azar, menos del 1 %).
                () => struggling = CareerAreaLives.StrugglingWorkerEvents(RepoPacks.Content, lives: 8),
                // Y una vida por carrera con las semillas de su carrera ya plantadas, para los
                // eventos que vienen de una semilla rara (por ejemplo, un reclamo de ética).
                () => careerSeeds = CareerAreaLives.CareerSeedEvents(RepoPacks.Content),
                // Y vidas en Chile que se separan con hijos chicos, para los caminos raros de la
                // pensión de alimentos y el cuidado personal (el juicio por los niños, el registro de deudores).
                () => separated = CareerAreaLives.SeparatedParentEvents(RepoPacks.Content, lives: 60),
                // Y vidas en Chile cuyos papás dejan ahorros, para la posesión efectiva de la herencia.
                () => inheritance = CareerAreaLives.InheritanceEvents(RepoPacks.Content, lives: 12),
                // Y vidas que ascienden cada vez que pueden, para llegar a lo más alto de la carrera.
                () => top = CareerAreaLives.TopOfCareerEvents(RepoPacks.Content, lives: 6),
                // Y vidas que trabajan por su cuenta, sin jefe ni colegas (casi nadie en una vida al azar).
                () => freelance = CareerAreaLives.FreelanceWorkerEvents(RepoPacks.Content, lives: 4),
                // Y vidas que viven con un amigo, para lo que pasa al compartir el arriendo.
                () => roommates = CareerAreaLives.RoommateEvents(RepoPacks.Content, lives: 40),
                // Y vidas donde la amistad se enfría, para que el amigo se aleje (en vidas al azar,
                // muy pocas: dependía de la suerte).
                () => drift = CareerAreaLives.FriendDriftEvents(RepoPacks.Content, lives: 10),
                // Y vidas donde la pareja acumula rencor, para que se vaya (en vidas al azar, menos
                // del 1 %: también dependía de la suerte).
                () => partner = CareerAreaLives.PartnerLeavesEvents(RepoPacks.Content, lives: 10));

            string[] never = young.NeverSeenEvents.Intersect(old.NeverSeenEvents).Except(careers).Except(sick).Except(struggling).Except(careerSeeds).Except(separated).Except(inheritance).Except(top).Except(freelance).Except(roommates).Except(drift).Except(partner).ToArray();

            Assert.Empty(never);
        }

        [Fact]
        public void ParejaConMuchoRencor_SeVa()
        {
            Assert.Contains("evento.pareja_se_va", CareerAreaLives.PartnerLeavesEvents(RepoPacks.Content, lives: 10));
        }

        [Fact]
        public void AmistadQueSeEnfria_ElAmigoSeAleja()
        {
            Assert.Contains("evento.amigo_se_aleja", CareerAreaLives.FriendDriftEvents(RepoPacks.Content, lives: 10));
        }

        [Fact]
        [Trait(TestKinds.Name, TestKinds.ManyLives)]
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

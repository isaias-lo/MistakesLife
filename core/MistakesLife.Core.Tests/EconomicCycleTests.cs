using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class EconomicCycleTests
    {
        /// <summary>
        /// País de prueba: inflación estable (3 %) o alta (20 %); ciclo normal o crisis,
        /// y la crisis trae inflación alta y hace caer los fondos un 25 %.
        /// </summary>
        private static ContentDatabase Content(string normalToCrisis = "0", string extraEvents = "")
        {
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.estable"", ""regimenes"": [
                { ""id"": ""economia.inflacion.estable"", ""anual_min"": 0.03, ""anual_max"": 0.03 },
                { ""id"": ""economia.inflacion.alta"", ""anual_min"": 0.2, ""anual_max"": 0.2 } ] },
              ""ciclo"": { ""fase_inicial"": ""economia.ciclo.normal"", ""fases"": [
                { ""id"": ""economia.ciclo.normal"", ""cambio_anual"": { ""economia.ciclo.crisis"": " + normalToCrisis + @" } },
                { ""id"": ""economia.ciclo.crisis"", ""inflacion"": ""economia.inflacion.alta"", ""golpe_fondos"": -0.25 } ] }
            } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            if (extraEvents.Length > 0)
            {
                content.AddEventsJson("pack.prueba", "eventos.json", extraEvents);
            }

            return content.Build();
        }

        private static Value Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        [Fact]
        public void AlEmpezar_ElPaisEstaEnSuFaseInicial()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Assert.Equal("economia.ciclo.normal", Eval(world, content, "mundo.ciclo").AsText("prueba"));
        }

        [Fact]
        public void CambiarCiclo_ACrisis_SubeLaInflacionYCaenLosFondos()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.PensionFund = 1000000;

            Run(world, content, "cambiar_ciclo('economia.ciclo.crisis')");

            Assert.Equal("economia.ciclo.crisis", Eval(world, content, "mundo.ciclo").AsText("prueba"));
            Assert.Equal("economia.inflacion.alta", Eval(world, content, "mundo.regimen_inflacion").AsText("prueba"));
            Assert.Equal(750000m, world.Player.PensionFund);
            Assert.Contains(world.Journal, e => e.TextKey == "diario.economia.ciclo.crisis");
        }

        [Fact]
        public void ConProbabilidadSegura_LaCrisisLlegaSola()
        {
            ContentDatabase content = Content(normalToCrisis: "1");
            World world = new LifeSession(content, 1).World;

            new WeekCycle().Register(new EconomySystem(content)).AdvanceWeeks(world, 1);

            Assert.Equal("economia.ciclo.crisis", Eval(world, content, "mundo.ciclo").AsText("prueba"));
        }

        [Fact]
        public void MismaPartida_MismoCiclo()
        {
            ContentDatabase content = Content(normalToCrisis: "0.3");
            World a = new LifeSession(content, 7).World;
            World b = new LifeSession(content, 7).World;

            new WeekCycle().Register(new EconomySystem(content)).AdvanceWeeks(a, 5 * GameDate.WeeksPerYear);
            new WeekCycle().Register(new EconomySystem(content)).AdvanceWeeks(b, 5 * GameDate.WeeksPerYear);

            Assert.Equal(a.EconomyOf("pais.prueba")!.PhaseId, b.EconomyOf("pais.prueba")!.PhaseId);
            Assert.Equal(a.EconomyOf("pais.prueba")!.PriceIndex, b.EconomyOf("pais.prueba")!.PriceIndex);
        }

        [Fact]
        public void CambiarCiclo_AUnaFaseQueNoExiste_EsUnErrorDeContenido()
        {
            string events = @"[ { ""id"": ""evento.prueba.crisis"", ""probabilidad_anual"": 1,
                ""efectos"": [ ""cambiar_ciclo('economia.ciclo.guerra')"" ] } ]";

            var ex = Assert.Throws<ContentLoadException>(() => Content(extraEvents: events));

            Assert.Contains(ex.Errors, e => e.Message.Contains("economia.ciclo.guerra"));
        }

        [Fact]
        public void Chile_TieneRecesionesYCrisisQueGolpeanLosFondos()
        {
            EconomicCycleDefinition cycle = RepoPacks.Content.GetCountry("pais.chile").Economy!.Cycle!;

            Assert.True(cycle.Phase("economia.ciclo.recesion")!.PensionFundShock < 0);
            Assert.Equal("economia.inflacion.alta", cycle.Phase("economia.ciclo.crisis")!.InflationRegime);
        }
    }
}

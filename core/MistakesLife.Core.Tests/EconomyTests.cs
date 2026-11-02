using System;
using System.Linq;
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
    public class EconomyTests
    {
        /// <summary>Un país de prueba con dos regímenes: "estable" (3 % fijo) y "loca" (100 % fijo).</summary>
        private const string Economy = @", ""economia"": { ""inflacion"": {
            ""regimen_inicial"": ""economia.inflacion.estable"",
            ""regimenes"": [
              { ""id"": ""economia.inflacion.estable"", ""anual_min"": 0.03, ""anual_max"": 0.03 },
              { ""id"": ""economia.inflacion.loca"", ""anual_min"": 1, ""anual_max"": 1 }
            ] } } }";

        private static ContentDatabase Content(string economy = Economy, string? eventsJson = null)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            if (eventsJson != null)
            {
                content.AddEventsJson("pack.prueba", "events/prueba.json", eventsJson);
            }

            return content.Build();
        }

        private static Value Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        /// <summary>Avanza un mundo solo con el sistema de economía (sin eventos que detengan el tiempo).</summary>
        private static void AdvanceEconomy(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void Mes_LasCincuentaYDosSemanasSeRepartenEnDoceMeses()
        {
            var starts = Enumerable.Range(0, GameDate.WeeksPerYear).Select(w => new GameDate(w)).Where(d => d.IsFirstWeekOfMonth).ToList();

            Assert.Equal(12, starts.Count);
            Assert.Equal(Enumerable.Range(1, 12), starts.Select(d => d.Month));
            Assert.Equal(1, new GameDate(0).Month);
            Assert.Equal(12, new GameDate(51).Month);
            Assert.True(new GameDate(52).IsFirstWeekOfMonth);
        }

        [Fact]
        public void Precio_AlEmpezar_EsElMismoMonto()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Assert.Equal(15000, Eval(world, content, "precio(15000)").AsNumber("prueba"));
            Assert.Equal("economia.inflacion.estable", Eval(world, content, "mundo.regimen_inflacion").AsText("prueba"));
        }

        [Fact]
        public void Inflacion_DespuesDeUnAnio_LosPreciosSubenLoQueDiceElRegimen()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            AdvanceEconomy(world, content, GameDate.WeeksPerYear);

            Assert.Equal(1.03, world.EconomyOf("pais.prueba")!.PriceIndex, 6);
            Assert.Equal(10300, Eval(world, content, "precio(10000)").AsNumber("prueba"));
            Assert.Equal(3.0, Eval(world, content, "mundo.inflacion").AsNumber("prueba"));
        }

        [Fact]
        public void Dinero_SeSumaYRestaConEfectos_YPuedeQuedarNegativo()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Run(world, content, "jugador.dinero += precio(20000)");
            Run(world, content, "jugador.dinero -= 50000");

            Assert.Equal(-30000m, world.Player.Money);
            Assert.True(Eval(world, content, "jugador.dinero < 0").AsBool("prueba"));
        }

        [Fact]
        public void Dinero_ConHiperinflacionDeMuchosAnios_NoSeDesborda()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "cambiar_inflacion('economia.inflacion.loca')");

            AdvanceEconomy(world, content, GameDate.WeeksPerYear * 40);
            Run(world, content, "jugador.dinero += precio(1000)");

            // 100 % al año durante 40 años: todo cuesta 2^40 veces más (un billón de veces).
            Assert.InRange(world.EconomyOf("pais.prueba")!.PriceIndex / Math.Pow(2, 40), 0.999999, 1.000001);
            Assert.True(world.Player.Money > 1_000_000_000_000_000m / 1000);
        }

        [Fact]
        public void CambiarInflacion_PasaAlRegimenYLoAnotaEnElDiario()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Run(world, content, "cambiar_inflacion('economia.inflacion.loca')");

            Assert.Equal("economia.inflacion.loca", world.EconomyOf("pais.prueba")!.RegimeId);
            Assert.Equal("diario.economia.inflacion.loca", world.Journal.Last().TextKey);
        }

        [Fact]
        public void CambioAnual_ConProbabilidadUno_CambiaDeRegimenAntesDeUnAnio()
        {
            const string economy = @", ""economia"": { ""inflacion"": {
                ""regimen_inicial"": ""economia.inflacion.estable"",
                ""regimenes"": [
                  { ""id"": ""economia.inflacion.estable"", ""anual_min"": 0.02, ""anual_max"": 0.04, ""variacion_mensual"": 0.01,
                    ""cambio_anual"": { ""economia.inflacion.alta"": 1 } },
                  { ""id"": ""economia.inflacion.alta"", ""anual_min"": 0.1, ""anual_max"": 0.2 }
                ] } } }";
            ContentDatabase content = Content(economy);
            World world = new LifeSession(content, 3).World;

            AdvanceEconomy(world, content, GameDate.WeeksPerYear);

            CountryEconomy state = world.EconomyOf("pais.prueba")!;
            Assert.Equal("economia.inflacion.alta", state.RegimeId);
            Assert.InRange(state.AnnualInflation, 0.1, 0.2);
        }

        [Fact]
        public void Inflacion_ConLaMismaSemilla_SeRepiteExacta()
        {
            ContentDatabase content = RepoPacks.Content;
            World a = new LifeSession(content, 77).World;
            World b = new LifeSession(content, 77).World;

            AdvanceEconomy(a, content, GameDate.WeeksPerYear * 30);
            AdvanceEconomy(b, content, GameDate.WeeksPerYear * 30);

            Assert.Equal(a.EconomyOf("pais.chile")!.PriceIndex, b.EconomyOf("pais.chile")!.PriceIndex);
        }

        [Fact]
        public void Chile_EnCienAnios_LaInflacionPromedioSePareceALaReal()
        {
            ContentDatabase content = RepoPacks.Content;
            const int years = 100;
            for (ulong seed = 1; seed <= 5; seed++)
            {
                World world = new LifeSession(content, seed).World;
                AdvanceEconomy(world, content, GameDate.WeeksPerYear * years);

                // Chile 2000-2024: cerca de 3,5 % al año en promedio (Banco Central, IPC del INE).
                double average = Math.Pow(world.EconomyOf("pais.chile")!.PriceIndex, 1.0 / years) - 1;
                Assert.InRange(average, 0.03, 0.055);
            }
        }

        [Fact]
        public void CambiarInflacion_AUnRegimenQueElPaisNoTiene_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content(eventsJson: @"[
              { ""id"": ""evento.crisis"", ""efectos"": [""cambiar_inflacion('economia.inflacion.hiper')""] }
            ]"));

            Assert.Contains(ex.Errors, e => e.Message.Contains("no tiene ese régimen de inflación"));
        }

        [Fact]
        public void RegimenMalEscrito_EsUnErrorDeContenido()
        {
            const string economy = @", ""economia"": { ""inflacion"": {
                ""regimen_inicial"": ""economia.inflacion.no_existe"",
                ""regimenes"": [
                  { ""id"": ""economia.inflacion.estable"", ""anual_min"": 0.05, ""anual_max"": 0.01,
                    ""cambio_anual"": { ""economia.inflacion.otra"": 2 } }
                ] } } }";

            var ex = Assert.Throws<ContentLoadException>(() => Content(economy));

            Assert.Contains(ex.Errors, e => e.Message.Contains("anual_max no menor que anual_min"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("no es otro régimen"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("regimen_inicial"));
        }
    }
}

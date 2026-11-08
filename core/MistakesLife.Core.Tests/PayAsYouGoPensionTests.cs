using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Jubilación de reparto (como la ANSES de Argentina): no hay cuenta propia,
    /// la pensión sale de los años de aportes y del sueldo de los últimos años.
    /// </summary>
    public class PayAsYouGoPensionTests
    {
        /// <summary>
        /// País de prueba sin inflación: aporte de 11 %; con 30 años de aportes se
        /// cobra 100.000 fijos + 1,5 % del sueldo promedio por año, y al menos 250.000.
        /// </summary>
        private const string Economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""sueldos"": { ""minimo"": 500000, ""tipico"": 800000 },
              ""liquidacion"": { ""dependiente"": [ { ""id"": ""descuento.prueba.jubilacion"", ""tasa"": 0.11 } ] },
              ""jubilacion"": {
                ""reparto"": { ""descuento"": ""descuento.prueba.jubilacion"", ""anos_minimos"": 30, ""porcentaje_por_ano"": 0.015,
                  ""anos_promedio"": 10, ""base_universal"": 100000, ""minima"": 250000 }
              } } }";

        private static readonly PayAsYouGoPensionDefinition Rules = new PayAsYouGoPensionDefinition
        {
            Deduction = "descuento.prueba.jubilacion",
            MinimumYears = 30,
            SharePerYear = 0.015,
            AverageYears = 10,
            UniversalBase = 100000,
            Minimum = 250000,
        };

        private static ContentDatabase Content(string economy = Economy)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new PayrollSystem(content)).Register(new PensionSystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void Reparto_SinLosAnosMinimos_NoHayPensionPropia()
        {
            Assert.Equal(0m, PensionSystem.PayAsYouGoPension(29 * 12 + 11, 1000000, Rules));
        }

        [Fact]
        public void Reparto_ConLosAnosMinimos_FijoMasUnPorcentajePorAno()
        {
            // 100.000 + 1,5 % × 35 años × 1.000.000 = 625.000.
            Assert.Equal(625000m, PensionSystem.PayAsYouGoPension(35 * 12, 1000000, Rules));
        }

        [Fact]
        public void Reparto_ConSueldoBajo_CobraLaMinima()
        {
            // 100.000 + 1,5 % × 30 × 300.000 = 235.000, menos que la mínima.
            Assert.Equal(250000m, PensionSystem.PayAsYouGoPension(30 * 12, 300000, Rules));
        }

        [Fact]
        public void SueldoPromedio_PrimerMesEsElSueldoYLuegoSeMueveDeAPoco()
        {
            var world = new LifeSession(Content(), 1).World;
            PensionSystem.AddToSalaryBase(world.Player, 1200000, Rules);
            Assert.Equal(1200000m, world.Player.PensionSalaryBase);

            // Un mes de 120: se mueve 1/120 de la diferencia.
            PensionSystem.AddToSalaryBase(world.Player, 2400000, Rules);
            Assert.Equal(1210000m, world.Player.PensionSalaryBase);
        }

        [Fact]
        public void TrabajoConContrato_AportaCadaMesSinCuentaPropia()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000)");

            // 9 semanas son 2 meses del juego: 2 sueldos.
            Advance(world, content, 9);

            Assert.Equal(2, world.Player.ContributedMonths);
            Assert.Equal(1000000m, world.Player.PensionSalaryBase);
            Assert.Equal(0m, world.Player.PensionFund);
            Assert.Equal(2 * 890000m, world.Player.Money);
        }

        [Fact]
        public void TrabajoInformal_NoSumaAnosNiSueldoPromedio()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000, 'informal')");

            Advance(world, content, 9);

            Assert.Equal(0, world.Player.ContributedMonths);
            Assert.Equal(0m, world.Player.PensionSalaryBase);
        }

        [Fact]
        public void Jubilarse_ConReparto_CobraSegunAnosYSueldo()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.ContributedMonths = 40 * 12;
            world.Player.PensionSalaryBase = 1000000;

            Run(world, content, "jugador.jubilarse()");
            Advance(world, content, 1);

            // 100.000 + 1,5 % × 40 × 1.000.000 = 700.000.
            Assert.Equal(700000m, world.Player.Pension);
            Assert.Equal(700000m, world.Player.Money);
        }

        [Fact]
        public void Pais_ConCuentaIndividualYReparto_EsUnError()
        {
            string both = Economy.Replace(@"""reparto"":", @"""cuenta_individual"": { ""descuento"": ""descuento.prueba.jubilacion"", ""anos_de_pension"": 1 }, ""reparto"":");
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + both);

            var error = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(error.Errors, e => e.Message.Contains("cuenta_individual o reparto, no los dos"));
        }

        [Fact]
        public void Reparto_ConUnDescuentoQueNoEstaEnLaLiquidacion_EsUnError()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}')
                + Economy.Replace(@"""descuento"": ""descuento.prueba.jubilacion""", @"""descuento"": ""descuento.prueba.otro"""));

            var error = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(error.Errors, e => e.Message.Contains("'descuento.prueba.otro' no está en liquidacion.dependiente"));
        }
    }
}

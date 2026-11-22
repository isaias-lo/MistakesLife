using System.Linq;
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
    public class UnemploymentInsuranceTests
    {
        /// <summary>
        /// País de prueba sin inflación. El trabajador aporta 10 % y el empleador
        /// <paramref name="employerRate"/> a la cuenta; se piden 3 cotizaciones; los pagos
        /// son 70 % y 50 % del sueldo promedio de 2 meses. El fondo común completa los
        /// 2 primeros pagos si el despido fue por necesidades de la empresa, con un
        /// primer pago de 100.000 a 600.000.
        /// </summary>
        private static ContentDatabase Content(double employerRate = 0.2)
        {
            string rate = employerRate.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""sueldos"": { ""minimo"": 500000, ""tipico"": 800000 },
              ""liquidacion"": { ""dependiente"": [ { ""id"": ""descuento.prueba.cesantia"", ""tasa"": 0.1 } ] },
              ""seguro_desempleo"": {
                ""descuento"": ""descuento.prueba.cesantia"", ""aporte_empleador_cuenta"": " + rate + @",
                ""cotizaciones_minimas"": 3, ""meses_promedio"": 2, ""pagos"": [0.7, 0.5],
                ""fondo_solidario"": { ""marcas"": [""despedido_necesidades""], ""meses_seguidos_ultimo_empleador"": 1, ""pagos"": 2,
                  ""minimo_primer_pago"": 100000, ""maximo_primer_pago"": 600000 },
                ""diario_primer_pago"": ""diario.prueba.primer_pago"", ""diario_ultimo_pago"": ""diario.prueba.ultimo_pago"",
                ""diario_sin_derecho"": ""diario.prueba.sin_derecho"", ""diario_retiro_al_jubilar"": ""diario.prueba.retiro""
              } } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static Value Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new PayrollSystem(content))
                .Register(new UnemploymentInsuranceSystem(content)).AdvanceWeeks(world, weeks);

        /// <summary>Trabaja con contrato por 1.000.000 hasta cobrar <paramref name="months"/> sueldos.</summary>
        private static World WorkMonths(ContentDatabase content, int months)
        {
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000)");
            Advance(world, content, 1);
            while (world.Player.UnemploymentContributions < months)
            {
                Advance(world, content, 1);
            }

            return world;
        }

        private static bool Wrote(World world, string key) => world.Journal.Any(j => j.TextKey == key);

        [Fact]
        public void SueldoConContrato_AhorraDescuentoYAporteDelEmpleadorEnLaCuenta()
        {
            ContentDatabase content = Content();
            World world = WorkMonths(content, 2);

            // Cada mes: 100.000 del trabajador y 200.000 del empleador.
            Assert.Equal(600000m, world.Player.UnemploymentFund);
            Assert.Equal(2, world.Player.UnemploymentContributions);
            Assert.Equal(1000000m, world.Player.UnemploymentSalaryBase);
            Assert.Equal(600000, Eval(world, content, "jugador.seguro_desempleo_saldo").AsNumber("prueba"));
        }

        [Fact]
        public void PerderLaPega_ConCotizaciones_CobraSoloCadaMesHastaElUltimoPago()
        {
            ContentDatabase content = Content();
            World world = WorkMonths(content, 4);
            decimal before = world.Player.Money;
            Run(world, content, "jugador.dejar_trabajo()");

            Advance(world, content, 1);
            Assert.NotNull(world.Player.UnemploymentClaim);
            Assert.Empty(world.Player.UnemploymentClaim!.Payments);

            Advance(world, content, 10);

            UnemploymentClaim claim = world.Player.UnemploymentClaim!;
            // 1.200.000 en la cuenta: 70 % y 50 % de 1.000.000; el segundo agota el saldo.
            Assert.Equal(new[] { 700000m, 500000m }, claim.Payments.Select(p => p.Amount));
            Assert.Equal(new[] { 1, 2 }, claim.Payments.Select(p => p.Number));
            Assert.All(claim.Payments, p => Assert.Equal(0m, p.FromSolidarityFund));
            Assert.True(claim.Finished);
            Assert.Equal(0m, world.Player.UnemploymentFund);
            Assert.Equal(before + 1200000m, world.Player.Money);
            Assert.True(Wrote(world, "diario.prueba.primer_pago"));
            Assert.True(Wrote(world, "diario.prueba.ultimo_pago"));
            Assert.False(Eval(world, content, "jugador.cobrando_seguro_desempleo").AsBool("prueba"));
        }

        [Fact]
        public void SaldoQueSobra_SigueConElUltimoPorcentajeMientrasAlcance()
        {
            ContentDatabase content = Content();
            World world = WorkMonths(content, 6);
            Run(world, content, "jugador.dejar_trabajo()");

            Advance(world, content, 20);

            // 1.800.000: 700.000, 500.000, 500.000 y lo que queda (100.000).
            Assert.Equal(new[] { 700000m, 500000m, 500000m, 100000m }, world.Player.UnemploymentClaim!.Payments.Select(p => p.Amount));
        }

        [Fact]
        public void SinLasCotizacionesMinimas_NoCobraYElDiarioLoDice()
        {
            ContentDatabase content = Content();
            World world = WorkMonths(content, 2);
            decimal before = world.Player.Money;
            Run(world, content, "jugador.dejar_trabajo()");

            Advance(world, content, 10);

            Assert.False(world.Player.UnemploymentClaim!.HasRight);
            Assert.Empty(world.Player.UnemploymentClaim.Payments);
            Assert.Equal(before, world.Player.Money);
            Assert.True(Wrote(world, "diario.prueba.sin_derecho"));
            // La plata sigue en su cuenta para cuando vuelva a cobrar.
            Assert.Equal(600000m, world.Player.UnemploymentFund);
        }

        [Fact]
        public void DespidoSinCulpa_ConPocoSaldo_ElFondoSolidarioCompletaConSuMaximo()
        {
            ContentDatabase content = Content(employerRate: 0);
            World world = WorkMonths(content, 3);
            Run(world, content, "jugador.marcar('despedido_necesidades')");
            Run(world, content, "jugador.dejar_trabajo()");

            Advance(world, content, 15);

            UnemploymentClaim claim = world.Player.UnemploymentClaim!;
            Assert.True(claim.Solidarity);
            // Pago 1: 70 % = 700.000 pasa el máximo de 600.000; la cuenta pone 300.000 y el fondo el resto.
            // Pago 2: 50 % = 500.000 pasa su máximo (600.000 × 50/70 = 428.571); todo del fondo.
            Assert.Equal(new[] { 600000m, 428571m }, claim.Payments.Select(p => p.Amount));
            Assert.Equal(new[] { 300000m, 428571m }, claim.Payments.Select(p => p.FromSolidarityFund));
            Assert.True(claim.Finished);
        }

        [Fact]
        public void RenunciarOEchadoPorFalta_ConPocoSaldo_SoloCobraLoDeSuCuenta()
        {
            ContentDatabase content = Content(employerRate: 0);
            World world = WorkMonths(content, 3);
            Run(world, content, "jugador.dejar_trabajo()");

            Advance(world, content, 15);

            UnemploymentClaim claim = world.Player.UnemploymentClaim!;
            Assert.False(claim.Solidarity);
            Assert.Equal(new[] { 300000m }, claim.Payments.Select(p => p.Amount));
            Assert.True(claim.Finished);
        }

        [Fact]
        public void TrabajoNuevo_TerminaElCobroYLasCotizacionesSeCuentanDeNuevo()
        {
            ContentDatabase content = Content();
            World world = WorkMonths(content, 6);
            Run(world, content, "jugador.dejar_trabajo()");
            Advance(world, content, 6);
            Assert.Single(world.Player.UnemploymentClaim!.Payments);
            Assert.True(Eval(world, content, "jugador.cobrando_seguro_desempleo").AsBool("prueba"));

            Run(world, content, "jugador.trabajar(1000000)");
            Advance(world, content, 1);

            Assert.Null(world.Player.UnemploymentClaim);
            Assert.False(Eval(world, content, "jugador.cobrando_seguro_desempleo").AsBool("prueba"));
            Assert.True(world.Player.UnemploymentContributions <= 1);
            Assert.True(world.Player.UnemploymentFund > 0);
        }

        [Fact]
        public void Jubilarse_RetiraDeUnaVezLoQueQuedabaEnLaCuenta()
        {
            ContentDatabase content = Content();
            World world = WorkMonths(content, 2);
            Run(world, content, "jugador.dejar_trabajo()");
            world.Player.Pension = 0;
            decimal before = world.Player.Money;

            Advance(world, content, 1);

            Assert.Equal(before + 600000m, world.Player.Money);
            Assert.Equal(0m, world.Player.UnemploymentFund);
            Assert.Null(world.Player.UnemploymentClaim);
            Assert.True(Wrote(world, "diario.prueba.retiro"));
        }

        [Fact]
        public void TrabajoInformal_NoCotizaNiCobra()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000, 'informal')");
            Advance(world, content, 20);
            Run(world, content, "jugador.dejar_trabajo()");
            Advance(world, content, 10);

            Assert.Equal(0m, world.Player.UnemploymentFund);
            Assert.Null(world.Player.UnemploymentClaim);
        }

        [Fact]
        public void GuardarYCargar_ConservaLaCuentaYElCobroConSusPagos()
        {
            ContentDatabase content = Content();
            var session = new LifeSession(content, 1);
            World world = session.World;
            Run(world, content, "jugador.trabajar(1000000)");
            Advance(world, content, 22);
            Run(world, content, "jugador.dejar_trabajo()");
            Advance(world, content, 6);
            Assert.NotEmpty(world.Player.UnemploymentClaim!.Payments);

            string saved = session.SaveToText();
            LifeSession loaded = LifeSession.Load(content, saved);

            Assert.Equal(saved, loaded.SaveToText());
            UnemploymentClaim claim = loaded.World.Player.UnemploymentClaim!;
            Assert.Equal(world.Player.UnemploymentClaim!.Payments.Select(p => p.Amount), claim.Payments.Select(p => p.Amount));
            Assert.Equal(world.Player.UnemploymentFund, loaded.World.Player.UnemploymentFund);
        }

        [Fact]
        public void PaqueteDeChile_TraeElSeguroDeCesantiaConSuDescuento()
        {
            UnemploymentInsuranceDefinition? insurance = RepoPacks.Content.GetCountry("pais.chile").Economy?.UnemploymentInsurance;

            Assert.NotNull(insurance);
            Assert.Equal("descuento.chile.seguro_cesantia", insurance!.Deduction);
            Assert.Equal(0.7, insurance.Payments[0]);
            Assert.NotNull(insurance.SolidarityFund);
        }
    }
}

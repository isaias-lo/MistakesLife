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
    public class TaxReturnTests
    {
        private static AnnualTaxDefinition ChileRules => RepoPacks.Content.GetCountry("pais.chile").Economy!.AnnualTax!;

        /// <summary>Semanas desde el inicio de un año hasta pasar la primera semana de abril (mes 4) del año siguiente.</summary>
        private const int UntilNextApril = GameDate.WeeksPerYear + 14;

        /// <summary>Semanas desde el inicio de un año hasta pasar la primera semana de mayo del año siguiente.</summary>
        private const int UntilNextMay = GameDate.WeeksPerYear + 19;

        /// <summary>País de prueba sin inflación, con honorarios retenidos al 10 %, impuesto anual de 20 % sobre 1.000.000 y multa de 10 %.</summary>
        private static ContentDatabase Content() => Build(@", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""sueldos"": { ""minimo"": 100000, ""tipico"": 200000 },
              ""liquidacion"": { ""honorarios"": [ { ""id"": ""descuento.prueba.retencion"", ""tasa"": 0.1 } ] },
              ""declaracion_anual"": { ""mes"": 4, ""multa_atraso"": 0.1,
                ""tramos"": [ { ""desde"": 0, ""tasa"": 0 }, { ""desde"": 1000000, ""tasa"": 0.2 } ] } } }");

        private static ContentDatabase Build(string economy)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle()
                .Register(new EconomySystem(content))
                .Register(new PayrollSystem(content))
                .Register(new TaxSystem(content))
                .AdvanceWeeks(world, weeks);

        [Fact]
        public void Declaracion_Chile_PsicologaAHonorarios_RecibeDevolucion()
        {
            // 1.100.000 al mes en boletas: 13.200.000 al año, con 15,25 % retenido.
            var income = new AnnualIncome(1);
            for (int i = 0; i < 12; i++)
            {
                income.Add(PayrollCalculator.Compute(GameDate.Start, JobKind.Freelance, 1100000, RepoPacks.Content.GetCountry("pais.chile").Economy!.Payroll, 1));
            }

            TaxReturn result = TaxReturnCalculator.Compute(income, ChileRules, 1);

            // Gastos presuntos 30 % (3.960.000) y cotizaciones 17 % sobre el 80 % (1.795.200): queda bajo el tramo exento.
            Assert.Equal(1795200, result.Contributions);
            Assert.Equal(0, result.Tax);
            Assert.Equal(2013000 - 1795200, result.Balance);
        }

        [Fact]
        public void Declaracion_Chile_HonorariosAltos_TocaPagar()
        {
            var income = new AnnualIncome(1) { };
            for (int i = 0; i < 12; i++)
            {
                income.Add(PayrollCalculator.Compute(GameDate.Start, JobKind.Freelance, 5000000, RepoPacks.Content.GetCountry("pais.chile").Economy!.Payroll, 1));
            }

            TaxReturn result = TaxReturnCalculator.Compute(income, ChileRules, 1);

            // Gastos presuntos con tope de 12.420.000; renta 60.000.000 - 12.420.000 - 8.160.000 = 39.420.000.
            Assert.Equal(39420000, result.TaxableIncome);
            Assert.Equal(546480 + 1166400, result.Tax);
            Assert.True(result.Balance < 0);
        }

        [Fact]
        public void Honorarios_EnAbril_TieneDeclaracionPendiente_YAlDeclararRecibeElSaldo()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(200000, 'honorarios')");

            Advance(world, content, UntilNextApril);

            TaxReturn pending = world.Player.PendingTaxReturn!;
            Assert.NotNull(pending);
            // 2.400.000 al año: impuesto 20 % de 1.400.000 = 280.000; retenido 240.000.
            Assert.Equal(-40000, pending.Balance);

            decimal before = world.Player.Money;
            Run(world, content, "jugador.declarar()");

            Assert.Equal(before - 40000, world.Player.Money);
            Assert.Null(world.Player.PendingTaxReturn);
            Assert.Equal(TaxSystem.PaymentJournal, world.Journal.Last().TextKey);
        }

        [Fact]
        public void NoDeclarar_ATiempo_PagaConMulta()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(200000, 'honorarios')");
            Advance(world, content, UntilNextApril);
            Run(world, content, "jugador.dejar_trabajo()");
            decimal before = world.Player.Money;

            Advance(world, content, UntilNextMay - UntilNextApril);

            Assert.Null(world.Player.PendingTaxReturn);
            Assert.Equal(before - 44000, world.Player.Money);
            Assert.Contains(world.Journal, j => j.TextKey == TaxSystem.PenaltyJournal);
        }

        [Fact]
        public void SoloConContrato_NoNecesitaDeclarar()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(200000)");

            Advance(world, content, UntilNextApril);

            Assert.Null(world.Player.PendingTaxReturn);
        }

        [Fact]
        public void DeclaracionMalEscrita_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Build(@", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""declaracion_anual"": { ""mes"": 13, ""tramos"": [ { ""desde"": 5, ""tasa"": 0 }, { ""desde"": 1, ""tasa"": 2 } ] } } }"));

            Assert.Contains(ex.Errors, e => e.Message.Contains("mes de 1 a 12"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("los tramos van de menor a mayor"));
        }
    }
}

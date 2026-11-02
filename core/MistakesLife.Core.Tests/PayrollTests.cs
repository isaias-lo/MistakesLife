using System.Linq;
using MistakesLife.Core.Characters;
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
    public class PayrollTests
    {
        private static PayrollDefinition ChileRules => RepoPacks.Content.GetCountry("pais.chile").Economy!.Payroll;

        /// <summary>País de prueba sin descuentos ni impuestos, con inflación fija.</summary>
        private static ContentDatabase Content(double inflation = 0, string payroll = "", string? eventsJson = null)
        {
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.fija"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.fija"", ""anual_min"": " + inflation + @", ""anual_max"": " + inflation + @" } ] },
              ""sueldos"": { ""minimo"": 500000, ""tipico"": 800000, ""relativo_sin_titulo"": 0.8, ""reajuste_meses"": 12 }" + payroll + @" } }";
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

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new PayrollSystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void Liquidacion_Chile_SueldoTipico_DescuentaAfpSaludYCesantiaSinImpuesto()
        {
            Payslip payslip = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 760000, ChileRules, 1);

            Assert.Equal(84740, payslip.Amount("descuento.chile.afp"));
            Assert.Equal(53200, payslip.Amount("descuento.chile.salud"));
            Assert.Equal(4560, payslip.Amount("descuento.chile.seguro_cesantia"));
            Assert.Equal(0, payslip.Amount(Payslip.IncomeTaxId));
            Assert.Equal(617500, payslip.Net);
        }

        [Fact]
        public void Liquidacion_Chile_SueldoAlto_PagaImpuestoPorTramos()
        {
            Payslip payslip = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 3000000, ChileRules, 1);

            // Base: 3.000.000 - 562.500 de cotizaciones = 2.437.500.
            // 4 % de 931.500 a 2.070.000 y 8 % de 2.070.000 a 2.437.500.
            Assert.Equal(45540 + 29400, payslip.Amount(Payslip.IncomeTaxId));
        }

        [Fact]
        public void Liquidacion_Chile_SobreElTope_CotizaSoloHastaElTope()
        {
            Payslip payslip = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 5000000, ChileRules, 1);

            Assert.Equal(390250, payslip.Amount("descuento.chile.afp"));
        }

        [Fact]
        public void Liquidacion_TopesYTramos_SubenConLosPrecios()
        {
            Payslip payslip = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 10000000, ChileRules, 2);

            // Con precios al doble, el tope de 3.500.000 vale 7.000.000.
            Assert.Equal(780500, payslip.Amount("descuento.chile.afp"));
        }

        [Fact]
        public void Liquidacion_Chile_Honorarios_SoloRetencion()
        {
            Payslip payslip = PayrollCalculator.Compute(GameDate.Start, JobKind.Freelance, 1000000, ChileRules, 1);

            Assert.Single(payslip.Lines);
            Assert.Equal(847500, payslip.Net);
        }

        [Fact]
        public void Trabajar_CadaMesRecibeElSueldoLiquido()
        {
            ContentDatabase content = Content(payroll: @", ""liquidacion"": { ""dependiente"": [ { ""id"": ""descuento.prueba"", ""tasa"": 0.1 } ] }");
            World world = new LifeSession(content, 1).World;

            Run(world, content, "jugador.trabajar(1000000)");
            Advance(world, content, GameDate.WeeksPerYear);

            Assert.Equal(12 * 900000m, world.Player.Money);
            Assert.Equal(900000, Eval(world, content, "jugador.sueldo_liquido").AsNumber("prueba"));
            Assert.Equal("dependiente", Eval(world, content, "jugador.tipo_trabajo").AsText("prueba"));
        }

        [Fact]
        public void DejarTrabajo_YaNoRecibeSueldo()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(sueldo_minimo())");
            Run(world, content, "jugador.dejar_trabajo()");

            Advance(world, content, GameDate.WeeksPerYear);

            Assert.Equal(0m, world.Player.Money);
            Assert.False(Eval(world, content, "jugador.trabaja").AsBool("prueba"));
        }

        [Fact]
        public void Reajuste_ConInflacionAlta_ElSueldoPierdeValorHastaQueSeReajusta()
        {
            ContentDatabase content = Content(inflation: 1);
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000)");

            Advance(world, content, 48);
            Assert.Equal(1000000m, world.Player.Job!.Salary);
            Assert.True(world.EconomyOf("pais.prueba")!.PriceIndex > 1.8);

            Advance(world, content, 4);
            Assert.Equal(2000000m, world.Player.Job!.Salary);
        }

        [Fact]
        public void SueldoTitulo_SinTitulo_EsElTipicoPorElRelativoDelPais()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Assert.Equal(640000, Eval(world, content, "sueldo_titulo()").AsNumber("prueba"));
        }

        [Fact]
        public void SueldoTitulo_ConTitulo_UsaElSueldoRelativoDeSuCarrera()
        {
            ContentDatabase content = RepoPacks.Content;
            World world = new LifeSession(content, 1).World;
            Character player = world.Player;
            player.Studies = new CareerStudies("carrera.software", "carrera.software.nivel.ingenieria_civil_computacion", "universidad", 11);
            player.Graduate();

            Assert.Equal(760000 * 3.0, Eval(world, content, "sueldo_titulo()").AsNumber("prueba"));
        }

        [Fact]
        public void EventoQueUsaSueldos_EnUnPaisSinSueldos_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson);
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[
              { ""id"": ""evento.trabajo"", ""efectos"": [""jugador.trabajar(sueldo_minimo())""] }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("no define sus sueldos"));
        }

        [Fact]
        public void Chile_TodosLosTrabajosDeLosPaquetes_TienenSueldo()
        {
            // Toda opción que marca 'trabajando' también debe empezar un trabajo con sueldo.
            foreach (EventDefinition ev in RepoPacks.Content.Events)
            {
                foreach (var effects in new[] { ev.Effects }.Concat(ev.Options.Select(o => o.Effects)))
                {
                    if (effects.Any(e => e.Contains("marcar('trabajando')") && !e.Contains("desmarcar")))
                    {
                        Assert.True(effects.Any(e => e.Contains("jugador.trabajar(")), $"{ev.Id} marca 'trabajando' sin jugador.trabajar().");
                    }
                }
            }
        }
    }
}

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
    public class DebtTests
    {
        /// <summary>
        /// País de prueba: un crédito de consumo sin interés en 10 cuotas y un crédito
        /// de estudios reajustable que paga a la institución y se cobra solo al
        /// dejar de estudiar. Moroso con 2 meses sin pagar.
        /// </summary>
        private static ContentDatabase Content(string inflation = "0", string extra = "")
        {
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.fija"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.fija"", ""anual_min"": " + inflation + @", ""anual_max"": " + inflation + @" } ] },
              ""deudas"": { ""meses_para_moroso"": 2, ""meses_prescripcion"": 24, ""tipos"": [
                { ""id"": ""deuda.prueba.consumo"", ""tasa_anual"": 0, ""cuotas"": 10 },
                { ""id"": ""deuda.prueba.estudios"", ""tasa_anual"": 0, ""cuotas"": 10, ""reajustable"": true, ""entrega_dinero"": false,
                  ""condicion_pago"": ""!jugador.tiene('estudiando')"" }
              ] } } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            if (extra.Length > 0)
            {
                content.AddEventsJson("pack.prueba", "eventos.json", extra);
            }

            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static double Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true)).AsNumber("prueba");

        private static bool Is(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).IsTrue(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new DebtSystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void Credito_EntregaElDineroYSeCobraEnCuotas()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Run(world, content, "jugador.endeudarse('deuda.prueba.consumo', 1000000)");
            Assert.Equal(1000000m, world.Player.Money);
            Assert.Equal(1000000, Eval(world, content, "jugador.deuda"));

            Advance(world, content, 1);

            Assert.Equal(900000m, world.Player.Money);
            Assert.Equal(900000, Eval(world, content, "jugador.deuda_de('deuda.prueba.consumo')"));
            Assert.Equal(100000, Eval(world, content, "jugador.cuotas_deudas"));
        }

        [Fact]
        public void Credito_AlPagarLaUltimaCuota_Desaparece()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.endeudarse('deuda.prueba.consumo', 1000000)");

            Advance(world, content, 52);

            Assert.Empty(world.Player.Debts);
            Assert.Equal(0m, world.Player.Money);
            Assert.Contains(world.Journal, e => e.TextKey == DebtSystem.PaidOffJournal);
        }

        [Fact]
        public void ConInteres_LaCuotaPagaElInteresYParteDeLaDeuda()
        {
            // 12 cuotas al 12 % anual: la cuota fija es mayor que 1/12 de lo que se debe.
            decimal installment = DebtSystem.Installment(1200000, 0.12, 12);

            Assert.InRange(installment, 106000m, 107000m);
        }

        [Fact]
        public void SinDinero_NoPagaYQuedaMoroso_YSaleAlPonerseAlDia()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.endeudarse('deuda.prueba.consumo', 1000000)");
            world.Player.Money = 0;

            Advance(world, content, 6);

            Assert.True(Is(world, content, "jugador.moroso"));
            Assert.Equal(1000000, Eval(world, content, "jugador.deuda"));
            Assert.Contains(world.Journal, e => e.TextKey == DebtSystem.DelinquentJournal);

            world.Player.Money = 500000;
            Advance(world, content, 4);

            Assert.False(Is(world, content, "jugador.moroso"));
            Assert.Contains(world.Journal, e => e.TextKey == DebtSystem.ClearedJournal);
        }

        [Fact]
        public void SinPagarPorAnios_LaDeudaPrescribe()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.endeudarse('deuda.prueba.consumo', 1000000)");
            world.Player.Money = 0;

            Advance(world, content, 2 * 52 + 4);

            Assert.Empty(world.Player.Debts);
            Assert.False(Is(world, content, "jugador.moroso"));
            Assert.Contains(world.Journal, e => e.TextKey == DebtSystem.PrescribedJournal);
        }

        [Fact]
        public void CreditoDeEstudios_NoEntregaDinero_YSeCobraAlTerminar()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.marcar('estudiando')");
            Run(world, content, "jugador.endeudarse('deuda.prueba.estudios', 1000000)");
            world.Player.Money = 500000;

            Advance(world, content, 10);

            Assert.Equal(500000m, world.Player.Money);
            Assert.False(Is(world, content, "jugador.moroso"));

            Run(world, content, "jugador.desmarcar('estudiando')");
            Advance(world, content, 4);

            Assert.True(world.Player.Money < 500000m);
        }

        [Fact]
        public void Inflacion_ReajustaSoloLasDeudasReajustables()
        {
            ContentDatabase content = Content(inflation: "1");
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.marcar('estudiando')");
            Run(world, content, "jugador.endeudarse('deuda.prueba.estudios', 1000000)");
            Run(world, content, "jugador.endeudarse('deuda.prueba.consumo', 1000000)");
            world.Player.Money = 0;

            Advance(world, content, 52);

            // Un año con 100 % de inflación: la reajustable se duplica; la otra no (y se queda impaga).
            Assert.InRange(world.Player.Debts.Single(d => d.TypeId == "deuda.prueba.estudios").Balance, 1900000m, 2100000m);
            Assert.Equal(1000000m, world.Player.Debts.Single(d => d.TypeId == "deuda.prueba.consumo").Balance);
        }

        [Fact]
        public void Endeudarse_ConUnaDeudaQueNoExisteEnElPais_EsUnErrorDeContenido()
        {
            string events = @"[ { ""id"": ""evento.prueba.prestamo"", ""probabilidad_anual"": 1,
                ""efectos"": [ ""jugador.endeudarse('deuda.prueba.hipoteca', 1000)"" ] } ]";

            var ex = Assert.Throws<ContentLoadException>(() => Content(extra: events));

            Assert.Contains(ex.Errors, e => e.Message.Contains("deuda.prueba.hipoteca"));
        }

        [Fact]
        public void Chile_CaeDeUnaCarrera_TieneCuotasQueCabenEnUnSueldoTipico()
        {
            DebtTypeDefinition cae = RepoPacks.Content.GetCountry("pais.chile").Economy!.Debts!.Type("deuda.chile.cae")!;
            decimal typical = RepoPacks.Content.GetCountry("pais.chile").Economy!.Salaries!.Typical;

            // 10 semestres a 1.100.000: la cuota mensual es menos del 15 % del sueldo típico.
            decimal installment = DebtSystem.Installment(11000000, cae.AnnualRate, cae.Installments);

            Assert.InRange(installment / typical, 0.05m, 0.15m);
        }
    }
}

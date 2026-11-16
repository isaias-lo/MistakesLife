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
    /// <summary>
    /// Salud real, parte 1 (paquete de Chile): Fonasa con sus tramos según lo que gana
    /// el jugador, los planes de isapre con la tabla de factores por edad (y lo que
    /// cuestan sobre el 7 % de la liquidación), y lo que se paga al atenderse según la
    /// previsión.
    /// </summary>
    public class HealthCareTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(World world, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static Value Eval(World world, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, Content, world.Player, null, readOnly: true));

        private static double Number(World world, string expression) => Eval(world, expression).AsNumber(expression);

        private static string Text(World world, string expression) => Eval(world, expression).AsText(expression);

        /// <summary>
        /// Una vida en Chile con esa edad. Solo pasa el calendario (sin sistemas), así
        /// los precios siguen en 1 y los montos se pueden comparar exactos.
        /// </summary>
        private static LifeSession AdultInChile(int age)
        {
            for (ulong seed = 1; seed < 100; seed++)
            {
                var session = new LifeSession(Content, seed);
                if (session.World.Player.Residence!.CountryId == "pais.chile")
                {
                    new WeekCycle().AdvanceWeeks(session.World, (age * GameDate.WeeksPerYear) + 2);
                    return session;
                }
            }

            throw new Xunit.Sdk.XunitException("Ninguna vida nació en Chile.");
        }

        [Fact]
        public void TodosEmpiezanEnFonasa_YSinIngresosQuedanEnElTramoA()
        {
            World world = AdultInChile(25).World;

            Assert.Equal("prevision.chile.fonasa", Text(world, "jugador.prevision"));
            Assert.False(Eval(world, "jugador.prevision_privada").AsBool("prueba"));
            Assert.Equal("prevision.chile.fonasa.tramo_a", Text(world, "jugador.tramo_salud"));
        }

        [Theory]
        [InlineData("jugador.trabajar(539000)", "prevision.chile.fonasa.tramo_b")]
        [InlineData("jugador.trabajar(700000)", "prevision.chile.fonasa.tramo_c")]
        [InlineData("jugador.trabajar(786940)", "prevision.chile.fonasa.tramo_c")]
        [InlineData("jugador.trabajar(900000)", "prevision.chile.fonasa.tramo_d")]
        [InlineData("jugador.trabajar(900000, 'honorarios')", "prevision.chile.fonasa.tramo_d")]
        [InlineData("jugador.trabajar(900000, 'informal')", "prevision.chile.fonasa.tramo_a")]
        public void ElTramoDeFonasa_SaleDeLoQueGanaConCotizaciones(string job, string tier)
        {
            World world = AdultInChile(30).World;

            Run(world, job);

            Assert.Equal(tier, Text(world, "jugador.tramo_salud"));
        }

        [Theory]
        [InlineData(22, 72553)]
        [InlineData(30, 80614)]
        [InlineData(40, 104798)]
        [InlineData(60, 161228)]
        [InlineData(70, 193474)]
        public void ElPlanDeIsapre_CuestaElPrecioBasePorElFactorDeLaEdad(int age, double price)
        {
            World world = AdultInChile(age).World;

            Assert.Equal(price, Number(world, "plan_salud('prevision.chile.isapre.preferente')"));
        }

        [Fact]
        public void Liquidacion_ConIsapreMasCaraQueEl7_DescuentaLaDiferencia()
        {
            PayrollDefinition rules = Content.GetCountry("pais.chile").Economy!.Payroll;

            Payslip payslip = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 760000, rules, 1,
                "descuento.chile.salud", "descuento.chile.salud_adicional", 80614);

            Assert.Equal(53200, payslip.Amount("descuento.chile.salud"));
            Assert.Equal(80614 - 53200, payslip.Amount("descuento.chile.salud_adicional"));
            Assert.Equal(617500 - (80614 - 53200), payslip.Net);
        }

        [Fact]
        public void Liquidacion_ConIsapreMasBarataQueEl7_SoloDescuentaEl7()
        {
            PayrollDefinition rules = Content.GetCountry("pais.chile").Economy!.Payroll;

            Payslip payslip = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 2000000, rules, 1,
                "descuento.chile.salud", "descuento.chile.salud_adicional", 80614);

            Assert.Equal(140000, payslip.Amount("descuento.chile.salud"));
            Assert.Equal(0, payslip.Amount("descuento.chile.salud_adicional"));
        }

        [Fact]
        public void Liquidacion_LaDiferenciaDeLaIsapre_RebajaImpuestoSoloHastaEl7DelTope()
        {
            PayrollDefinition rules = Content.GetCountry("pais.chile").Economy!.Payroll;

            Payslip withoutPlan = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 3000000, rules, 1);
            Payslip withPlan = PayrollCalculator.Compute(GameDate.Start, JobKind.Employee, 3000000, rules, 1,
                "descuento.chile.salud", "descuento.chile.salud_adicional", 300000);

            // El 7 % de 3.000.000 es 210.000; el plan cuesta 90.000 más, pero solo hasta el
            // 7 % del tope (245.000) rebaja el impuesto: 35.000.
            Assert.Equal(90000, withPlan.Amount("descuento.chile.salud_adicional"));
            Assert.Equal(withoutPlan.Taxable - 35000, withPlan.Taxable);
        }

        [Fact]
        public void ConContratoEIsapre_ElSueldoTraeLaDiferenciaDelPlan()
        {
            World world = AdultInChile(30).World;
            Run(world, "jugador.trabajar(760000)");
            Run(world, "jugador.afiliarse('prevision.chile.isapre.preferente')");

            new WeekCycle().Register(new PayrollSystem(Content)).Register(new ExpenseSystem(Content)).AdvanceWeeks(world, 5);

            Assert.True(Eval(world, "jugador.prevision_privada").AsBool("prueba"));
            Assert.Equal(80614 - 53200, world.Player.LastPayslip!.Amount("descuento.chile.salud_adicional"));
            Assert.DoesNotContain(world.Player.LastExpenses, l => l.Id == "prevision.chile.isapre.preferente");
        }

        [Fact]
        public void SinContratoConIsapre_ElPlanSePagaEnteroCadaMes()
        {
            World world = AdultInChile(30).World;
            Run(world, "jugador.afiliarse('prevision.chile.isapre.libre_eleccion')");

            new WeekCycle().Register(new ExpenseSystem(Content)).AdvanceWeeks(world, 5);

            Assert.Contains(world.Player.LastExpenses, l => l.Id == "prevision.chile.isapre.libre_eleccion" && l.Amount == 112860);
            Assert.Equal(112860, Number(world, "jugador.precio_plan_salud"));
        }

        [Fact]
        public void AHonorariosConIsapre_SoloPagaLoQueNoCubreLaRetencion()
        {
            World world = AdultInChile(30).World;
            Run(world, "jugador.trabajar(1000000, 'honorarios')");
            Run(world, "jugador.afiliarse('prevision.chile.isapre.preferente')");

            new WeekCycle().Register(new ExpenseSystem(Content)).AdvanceWeeks(world, 5);

            // La retención ya lleva el 7 % del 80 % de lo boleteado: 56.000.
            Assert.Contains(world.Player.LastExpenses, l => l.Id == "prevision.chile.isapre.preferente" && l.Amount == 80614 - 56000);
        }

        [Fact]
        public void VolverAFonasa_DejaDeCobrarElPlan()
        {
            World world = AdultInChile(30).World;
            Run(world, "jugador.afiliarse('prevision.chile.isapre.preferente')");

            Run(world, "jugador.afiliarse('prevision.chile.fonasa')");
            new WeekCycle().Register(new ExpenseSystem(Content)).AdvanceWeeks(world, 5);

            Assert.Null(world.Player.HealthPlanId);
            Assert.Equal(0, Number(world, "jugador.precio_plan_salud"));
            Assert.DoesNotContain(world.Player.LastExpenses, l => l.Id.StartsWith("prevision."));
        }

        [Theory]
        [InlineData(null, "atencion.chile.consulta_particular", 32500)]
        [InlineData("jugador.trabajar(600000)", "atencion.chile.consulta_particular", 7440)]
        [InlineData("jugador.afiliarse('prevision.chile.isapre.preferente')", "atencion.chile.consulta_particular", 3250)]
        [InlineData("jugador.afiliarse('prevision.chile.isapre.libre_eleccion')", "atencion.chile.consulta_particular", 9750)]
        [InlineData("jugador.trabajar(600000)", "atencion.chile.urgencia_clinica", 54450)]
        [InlineData("jugador.afiliarse('prevision.chile.isapre.preferente')", "atencion.chile.urgencia_clinica", 5445)]
        [InlineData(null, "atencion.chile.cesfam", 0)]
        public void ElCopago_DependeDeLaPrevision(string? setup, string attention, double copay)
        {
            World world = AdultInChile(30).World;
            if (setup != null)
            {
                Run(world, setup);
            }

            Assert.Equal(copay, Number(world, $"copago('{attention}')"));
        }

        [Fact]
        public void Atenderse_PagaElCopago_QuedaEnLaCartolaYSeAnotaLaFecha()
        {
            World world = AdultInChile(30).World;
            Run(world, "jugador.trabajar(600000)");
            Assert.Equal(CharacterObject.NeverAttendedWeeks, Number(world, "jugador.semanas_desde_atencion"));

            Run(world, "jugador.atenderse('atencion.chile.consulta_particular')");

            Assert.Equal(-7440m, world.Player.Money);
            Assert.Contains(world.Player.LastExpenses, l => l.Id == "atencion.chile.consulta_particular" && l.Amount == 7440);
            Assert.Equal(0, Number(world, "jugador.semanas_desde_atencion"));
        }

        [Fact]
        public void ElConsultorio_EsSoloParaQuienEstaEnFonasa()
        {
            World world = AdultInChile(30).World;
            Assert.True(Eval(world, "jugador.puede_atenderse('atencion.chile.cesfam')").AsBool("prueba"));

            Run(world, "jugador.afiliarse('prevision.chile.isapre.preferente')");

            Assert.False(Eval(world, "jugador.puede_atenderse('atencion.chile.cesfam')").AsBool("prueba"));
            Assert.True(Eval(world, "jugador.puede_atenderse('atencion.chile.urgencia_clinica')").AsBool("prueba"));
            Assert.Throws<ScriptException>(() => Run(world, "jugador.atenderse('atencion.chile.cesfam')"));
        }

        [Fact]
        public void LaPrevisionYLaUltimaAtencion_SeGuardanConLaPartida()
        {
            LifeSession session = AdultInChile(30);
            Run(session.World, "jugador.afiliarse('prevision.chile.isapre.preferente')");
            Run(session.World, "jugador.atenderse('atencion.chile.urgencia_clinica')");

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());

            Assert.Equal("prevision.chile.isapre.preferente", loaded.World.Player.HealthPlanId);
            Assert.Equal(session.World.Player.LastHealthCareDate, loaded.World.Player.LastHealthCareDate);
        }

        [Fact]
        public void UnEventoQueNombraUnaPrevisionQueNoExiste_EsUnError()
        {
            var content = new ContentDatabase();
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""liquidacion"": { ""dependiente"": [ { ""id"": ""descuento.prueba.salud"", ""tasa"": 0.07 } ] },
              ""salud"": { ""publico"": ""prevision.prueba.publica"", ""sin_ingresos"": ""prevision.prueba.publica.sin_ingresos"",
                ""tramos"": [ { ""id"": ""prevision.prueba.publica.unico"" } ],
                ""descuento"": ""descuento.prueba.salud"", ""descuento_adicional"": ""descuento.prueba.salud_extra"" } } }";
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            content.AddEventsJson("pack.prueba", "eventos.json", @"[ { ""id"": ""evento.prueba_salud"", ""probabilidad_anual"": 1, ""opciones"": [
              { ""texto"": ""opcion.prueba"", ""efectos"": [ ""jugador.afiliarse('prevision.prueba.privada')"", ""jugador.atenderse('atencion.prueba.consulta')"" ] } ] } ]");

            ContentLoadException error = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(error.Errors, e => e.Message.Contains("prevision.prueba.privada"));
            Assert.Contains(error.Errors, e => e.Message.Contains("atencion.prueba.consulta"));
        }

        [Fact]
        public void Chile_LasIsapresNoAparecenSiTieneEnfermedadesPreexistentes()
        {
            World world = AdultInChile(30).World;
            Run(world, "jugador.marcar('diabetes')");
            EventDefinition ev = Content.GetEvent("evento.chile.elegir_prevision");
            var scope = new EventScope(world, Content, world.Player, null, readOnly: true);

            var open = ev.Options.Where(o => o.CompiledConditions == null || o.CompiledConditions.IsTrue(scope)).Select(o => o.TextKey).ToList();

            Assert.Equal(new[] { "opcion.elegir_prevision.fonasa" }, open);
        }
    }
}

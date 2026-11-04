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
    public class PensionTests
    {
        /// <summary>
        /// País de prueba sin inflación: la AFP descuenta 10 % y todo va a la cuenta;
        /// la pensión dura 1 año (12 meses) y la pensión del Estado es de 100.000,
        /// completa hasta una pensión propia de 50.000 y nada desde 150.000.
        /// </summary>
        private static ContentDatabase Content(double annualReturn = 0, string deduction = "descuento.prueba.afp")
        {
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""sueldos"": { ""minimo"": 500000, ""tipico"": 800000 },
              ""liquidacion"": { ""dependiente"": [ { ""id"": ""descuento.prueba.afp"", ""tasa"": 0.1 } ] },
              ""jubilacion"": {
                ""cuenta_individual"": { ""descuento"": """ + deduction + @""", ""tasa_honorarios"": 0.1,
                  ""rentabilidad_real_anual"": " + annualReturn.ToString(System.Globalization.CultureInfo.InvariantCulture) + @", ""anos_de_pension"": 1 },
                ""pension_universal"": { ""monto"": 100000, ""completa_hasta"": 50000, ""sin_pension_desde"": 150000 }
              } } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static double Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true)).AsNumber("prueba");

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new PayrollSystem(content)).Register(new PensionSystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void TrabajoInformal_RecibeTodoElSueldoPeroNoCotizaNiSumaMeses()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000, 'informal')");

            // 9 semanas son 2 meses del juego: 2 sueldos.
            Advance(world, content, 9);

            Assert.Equal(0m, world.Player.PensionFund);
            Assert.Equal(0, world.Player.ContributedMonths);
            Assert.Equal(0, Eval(world, content, "jugador.meses_cotizados"));
            Assert.Equal(2000000m, world.Player.Money);
            Assert.Null(world.Player.IncomeOf(world.Date.Year));
            Assert.Equal("informal", Expression.Parse("jugador.tipo_trabajo").Evaluate(new EventScope(world, content, world.Player, null, readOnly: true)).AsText("prueba"));
        }

        [Fact]
        public void TrabajoConContrato_CadaMesCotizadoSuma()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000)");

            Advance(world, content, 9);

            Assert.Equal(2, world.Player.ContributedMonths);
            Assert.Equal(2, Eval(world, content, "jugador.meses_cotizados"));
        }

        [Fact]
        public void Cotizacion_ConContrato_SeAhorraEnElFondo()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.trabajar(1000000)");

            Advance(world, content, 1);

            Assert.Equal(100000m, world.Player.PensionFund);
            Assert.Equal(100000, Eval(world, content, "jugador.fondo_pension"));
            Assert.Equal(900000m, world.Player.Money);
        }

        [Fact]
        public void Fondo_GanaSuRentabilidadCadaMes()
        {
            ContentDatabase content = Content(annualReturn: 0.12);
            World world = new LifeSession(content, 1).World;
            world.Player.PensionFund = 1000000;

            Advance(world, content, GameDate.WeeksPerYear);

            // Doce meses al 12 % anual: crece 12 %.
            Assert.InRange(world.Player.PensionFund, 1119999m, 1120001m);
        }

        [Fact]
        public void Jubilarse_ConvierteElFondoEnUnaPensionMensual()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.PensionFund = 1200000;
            Assert.Equal(100000, Eval(world, content, "jugador.pension_estimada"));

            Run(world, content, "jugador.jubilarse()");
            Advance(world, content, 1);

            Assert.Equal(1, Eval(world, content, "si(jugador.jubilado, 1, 0)"));
            Assert.Equal(0m, world.Player.PensionFund);
            Assert.Equal(100000m, world.Player.Money);
            Assert.Equal(100000, Eval(world, content, "jugador.pension"));
        }

        [Fact]
        public void Jubilarse_DosVeces_EsUnError()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.jubilarse()");

            Assert.Throws<ScriptException>(() => Run(world, content, "jugador.jubilarse()"));
        }

        [Fact]
        public void PensionUniversal_BajaMientrasMayorEsLaPensionPropia()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.PensionFund = 1200000;
            Run(world, content, "jugador.jubilarse()");
            Run(world, content, "jugador.recibir_pension_universal()");

            Advance(world, content, 1);

            // Pensión propia de 100.000: a medio camino entre 50.000 y 150.000, recibe la mitad.
            Assert.Equal(150000m, world.Player.Money);

            UniversalPensionDefinition rules = content.GetCountry("pais.prueba").Economy!.Pensions!.UniversalPension!;
            Assert.Equal(100000m, PensionCalculator.UniversalPension(0, rules));
            Assert.Equal(0m, PensionCalculator.UniversalPension(200000, rules));
        }

        [Fact]
        public void SinPedirla_NoRecibePensionUniversal()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
        }

        [Fact]
        public void Honorarios_AhorranParaLaPensionAlDeclarar()
        {
            var annual = new AnnualTaxDefinition { Month = 4, FreelanceContributionBase = 0.8 };
            var rules = new PensionsDefinition { IndividualAccount = new PensionAccountDefinition { Deduction = "descuento.prueba.afp", FreelanceRate = 0.1, PensionYears = 1 } };
            var income = new AnnualIncome(1);
            income.Add(PayrollCalculator.Compute(new GameDate(0), JobKind.Freelance, 1000000, new PayrollDefinition(), 1));
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            PensionSystem.SaveFreelance(world.Player, rules, annual, income, 1);

            // Cotiza sobre el 80 % de lo ganado: 10 % de 800.000.
            Assert.Equal(80000m, world.Player.PensionFund);
        }

        [Fact]
        public void Cuenta_ConUnDescuentoQueNoExiste_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content(deduction: "descuento.prueba.otro"));

            Assert.Contains(ex.Errors, e => e.Message.Contains("descuento.prueba.otro"));
        }

        [Fact]
        public void Chile_VidaLaboralCompletaConSueldoTipico_PensionCercanaALaMitadDelSueldo()
        {
            // 40 años cotizando sobre el sueldo típico (dinero del inicio, sin inflación).
            PensionsDefinition rules = RepoPacks.Content.GetCountry("pais.chile").Economy!.Pensions!;
            PensionAccountDefinition account = rules.IndividualAccount!;
            decimal salary = RepoPacks.Content.GetCountry("pais.chile").Economy!.Salaries!.Typical;
            decimal fund = 0;
            decimal monthly = 1 + (decimal)PensionCalculator.MonthlyReturn(account.RealAnnualReturn);
            for (int month = 0; month < 40 * 12; month++)
            {
                fund = fund * monthly + salary * 0.1115m * (decimal)account.ShareToAccount;
            }

            decimal pension = PensionCalculator.MonthlyPension(fund, account);

            Assert.InRange(pension / salary, 0.35m, 0.6m);
            // Con esa pensión todavía recibe la PGU completa.
            Assert.Equal(rules.UniversalPension!.Amount, PensionCalculator.UniversalPension(pension, rules.UniversalPension));
        }
    }
}

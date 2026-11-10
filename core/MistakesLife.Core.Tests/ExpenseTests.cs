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
    public class ExpenseTests
    {
        /// <summary>Semanas hasta pasar la primera semana del segundo mes (dos cobros: mes 1 y mes 2).</summary>
        private const int TwoMonths = 6;

        /// <summary>Arriendos de prueba de cada comuna: un departamento y una pieza con las cuentas incluidas.</summary>
        private const string Rents = @", ""arriendos"": { ""vivienda.departamento"": 300000, ""vivienda.pieza"": 120000 }";

        /// <summary>País de prueba sin inflación: arriendo del contrato (compartido con la pareja) y comida por hijo.</summary>
        private static ContentDatabase Content(int fromAge = 0, string rent = Rents) => Build(fromAge, rent);

        /// <summary>Línea de crédito de prueba, sin interés: cubre lo que falta hasta 150.000 y se paga con el saldo.</summary>
        private const string CreditLine = @", ""deuda_por_deficit"": ""deuda.prueba.linea"", ""cupo_deficit"": 150000";

        private const string Debts = @", ""deudas"": { ""meses_para_moroso"": 3, ""tipos"": [
                { ""id"": ""deuda.prueba.linea"", ""tasa_anual"": 0, ""cuotas"": 1, ""pago_minimo"": 0.05, ""pago_minimo_monto"": 0, ""se_paga_con_el_saldo"": true } ] }";

        private static ContentDatabase Build(int fromAge, string rent, string line = "", string debts = "")
        {
            string country = CountryTests.CountryJson
                .Replace(@"""nivel_hogar_max"": 40 }", @"""nivel_hogar_max"": 40" + rent + " }")
                .Replace(@"""nivel_hogar_max"": 90 }", @"""nivel_hogar_max"": 90" + rent + " }");
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""vivienda"": { ""plazo_meses"": 12, ""garantia_meses"": 1, ""reajuste_meses"": 12, ""tipos"": [
                { ""id"": ""vivienda.departamento"" }, { ""id"": ""vivienda.pieza"", ""incluye_cuentas"": true } ] },
              ""gastos"": { ""edad_desde"": " + fromAge + @", ""felicidad_en_rojo"": 5" + line + @", ""categorias"": [
                { ""id"": ""gasto.prueba.comida"", ""monto"": ""100000 + 50000 * jugador.hijos_menores"" }
              ] }" + debts + " } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", country.TrimEnd().TrimEnd('}') + economy);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static double Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true)).AsNumber("prueba");

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new ExpenseSystem(content)).AdvanceWeeks(world, weeks);

        private static void AdvanceWithDebts(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new ExpenseSystem(content)).Register(new DebtSystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void Gastos_CadaMes_SeDescuentanSegunSusCondiciones()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.dinero += 1000000");

            Advance(world, content, TwoMonths);

            // Vive con sus padres: solo paga la comida, dos meses.
            Assert.Equal(800000m, world.Player.Money);
            Assert.Equal(100000, Eval(world, content, "jugador.gastos"));
        }

        [Fact]
        public void Arriendo_ViviendoIndependiente_SeDivideConLaPareja()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.dinero += 1000000");
            Run(world, content, "jugador.arrendar('vivienda.departamento')");
            Run(world, content, "jugador.formar_pareja()");

            Advance(world, content, 1);

            // Garantía de un mes al firmar, y después la mitad del arriendo más la comida.
            Assert.Equal(1000000m - 300000 - 150000 - 100000, world.Player.Money);
        }

        [Fact]
        public void Hijos_AumentanLosGastos()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.tener_hijo()");

            Advance(world, content, 1);

            Assert.Equal(1, Eval(world, content, "jugador.hijos_menores"));
            Assert.Equal(-150000m, world.Player.Money);
        }

        [Fact]
        public void SinPlata_CadaMesEnRojo_BajaLaFelicidad()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            int happiness = world.Player.Stats.Happiness;

            Advance(world, content, TwoMonths);

            Assert.Equal(2, Eval(world, content, "jugador.meses_en_rojo"));
            Assert.Equal(System.Math.Max(0, happiness - 10), world.Player.Stats.Happiness);

            Run(world, content, "jugador.dinero += 1000000");
            Advance(world, content, 4);
            Assert.Equal(0, Eval(world, content, "jugador.meses_en_rojo"));
        }

        [Fact]
        public void AntesDeLaEdad_NoPagaGastos()
        {
            ContentDatabase content = Content(fromAge: 18);
            World world = new LifeSession(content, 1).World;

            Advance(world, content, TwoMonths);

            Assert.Equal(0m, world.Player.Money);
        }

        [Fact]
        public void Arriendo_EnUnaComunaSinArriendo_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content(rent: string.Empty));

            Assert.Contains(ex.Errors, e => e.Message.Contains("no tiene el arriendo de vivienda.departamento"));
        }

        [Fact]
        public void Chile_VivirSoloConElSueldoTipico_NoAlcanza()
        {
            // Lo que muestra la realidad de Santiago: un arriendo en Puente Alto se lleva
            // más de la mitad del sueldo típico líquido.
            ContentDatabase content = RepoPacks.Content;
            ExpensesDefinition rules = content.GetCountry("pais.chile").Economy!.Expenses!;
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.arrendar('vivienda.departamento')");
            Run(world, content, "jugador.trabajar(sueldo_tipico())");
            double alone = Monthly(world, content, rules);

            Assert.True(alone > 617500, $"Viviendo solo gasta {alone}, más que el líquido típico.");
        }

        [Fact]
        public void SinPlata_LaLineaDeCreditoCubreLoQueFalta_YElDineroNoQuedaEnNegativo()
        {
            ContentDatabase content = Build(0, Rents, CreditLine, Debts);
            World world = new LifeSession(content, 1).World;

            AdvanceWithDebts(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
            Assert.Equal(100000m, world.Player.Debts.Single(d => d.TypeId == "deuda.prueba.linea").Balance);
            Assert.Equal(1, Eval(world, content, "jugador.meses_en_rojo"));
            Assert.Contains(world.Journal, e => e.TextKey == ExpenseSystem.DeficitJournal);
        }

        [Fact]
        public void LineaDeCredito_NoPasaDeSuCupo_YLoDemasQuedaSinPagar()
        {
            ContentDatabase content = Build(0, Rents, CreditLine, Debts);
            World world = new LifeSession(content, 1).World;

            AdvanceWithDebts(world, content, 14);

            Assert.Equal(0m, world.Player.Money);
            Assert.Equal(150000m, world.Player.Debts.Single().Balance);
            Assert.Equal(1, world.Journal.Count(e => e.TextKey == ExpenseSystem.DeficitJournal));
        }

        [Fact]
        public void LineaDeCredito_AlLlegarPlata_SePagaSolaConElSaldo()
        {
            ContentDatabase content = Build(0, Rents, CreditLine, Debts);
            World world = new LifeSession(content, 1).World;
            AdvanceWithDebts(world, content, 1);

            Run(world, content, "jugador.dinero += 1000000");
            AdvanceWithDebts(world, content, TwoMonths - 1);

            // Paga la comida del mes (100.000) y la línea entera (100.000).
            Assert.Equal(800000m, world.Player.Money);
            Assert.Empty(world.Player.Debts);
            Assert.Equal(0, Eval(world, content, "jugador.meses_en_rojo"));
        }

        [Fact]
        public void SinLineaDeCredito_ElDineroQuedaEnNegativoComoAntes()
        {
            ContentDatabase content = Build(0, Rents, debts: Debts);
            World world = new LifeSession(content, 1).World;

            AdvanceWithDebts(world, content, 1);

            Assert.Equal(-100000m, world.Player.Money);
            Assert.Empty(world.Player.Debts);
        }

        [Fact]
        public void LineaDeCredito_QueNoExisteEnLasDeudas_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Build(0, Rents, CreditLine));

            Assert.Contains(ex.Errors, e => e.Message.Contains("no está en economia.deudas"));
        }

        [Theory]
        [InlineData("pais.chile")]
        [InlineData("pais.argentina")]
        public void ArrendarUnaPieza_CuestaMuchoMenosQueVivirSoloEnDepartamento(string countryId)
        {
            ContentDatabase content = RepoPacks.Content;
            ExpensesDefinition rules = content.GetCountry(countryId).Economy!.Expenses!;
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.arrendar('vivienda.departamento')");
            double alone = Monthly(world, content, rules);

            Run(world, content, "jugador.arrendar('vivienda.pieza')");
            double room = Monthly(world, content, rules);

            Assert.NotNull(rules.DeficitDebt);
            // La pieza ahorra arriendo y cuentas; la comida y la micro cuestan lo mismo.
            Assert.True(room < alone * 0.8, $"En pieza gasta {room}, viviendo solo {alone}.");
        }

        private static double Monthly(World world, ContentDatabase content, ExpensesDefinition rules)
        {
            var scope = new EventScope(world, content, world.Player, null, readOnly: true);
            return Eval(world, content, "jugador.arriendo") + rules.Categories.Where(c => c.CompiledConditions == null || c.CompiledConditions.IsTrue(scope))
                .Sum(c => c.CompiledAmount!.Evaluate(scope).AsNumber("prueba"));
        }
    }
}

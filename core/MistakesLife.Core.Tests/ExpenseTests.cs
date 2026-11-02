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

        /// <summary>País de prueba sin inflación: arriendo de la comuna (si vive independiente, compartido) y comida por hijo.</summary>
        private static ContentDatabase Content(int fromAge = 0, string rent = @", ""arriendo"": 300000") => Build(fromAge, rent);

        private static ContentDatabase Build(int fromAge, string rent)
        {
            string country = CountryTests.CountryJson
                .Replace(@"""nivel_hogar_max"": 40 }", @"""nivel_hogar_max"": 40" + rent + " }")
                .Replace(@"""nivel_hogar_max"": 90 }", @"""nivel_hogar_max"": 90" + rent + " }");
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""gastos"": { ""edad_desde"": " + fromAge + @", ""felicidad_en_rojo"": 5, ""categorias"": [
                { ""id"": ""gasto.prueba.arriendo"", ""monto"": ""arriendo()"", ""condiciones"": ""jugador.tiene('vive_independiente')"", ""compartido"": true },
                { ""id"": ""gasto.prueba.comida"", ""monto"": ""100000 + 50000 * jugador.hijos_menores"" }
              ] } } }";
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
            Run(world, content, "jugador.marcar('vive_independiente')");
            Run(world, content, "jugador.formar_pareja()");

            Advance(world, content, 1);

            Assert.Equal(1000000m - 150000 - 100000, world.Player.Money);
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

            Assert.Contains(ex.Errors, e => e.Message.Contains("no define su arriendo"));
        }

        [Fact]
        public void Chile_VivirSoloConElSueldoTipico_NoAlcanza()
        {
            // Lo que muestra la realidad de Santiago: un arriendo en Puente Alto se lleva
            // más de la mitad del sueldo típico líquido.
            ContentDatabase content = RepoPacks.Content;
            ExpensesDefinition rules = content.GetCountry("pais.chile").Economy!.Expenses!;
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.marcar('vive_independiente')");
            Run(world, content, "jugador.trabajar(sueldo_tipico())");
            var scope = new EventScope(world, content, world.Player, null, readOnly: true);
            double alone = rules.Categories.Where(c => c.CompiledConditions == null || c.CompiledConditions.IsTrue(scope))
                .Sum(c => c.CompiledAmount!.Evaluate(scope).AsNumber("prueba"));

            Assert.True(alone > 617500, $"Viviendo solo gasta {alone}, más que el líquido típico.");
        }
    }
}

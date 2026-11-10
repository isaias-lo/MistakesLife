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
    public class GroceryTests
    {
        /// <summary>País de prueba sin inflación: compra justa a 60.000 y básica a 90.000 por persona; un hijo come la mitad.</summary>
        private static ContentDatabase Content()
        {
            const string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""supermercado"": { ""factor_hijo"": 0.5, ""compras"": [
                { ""id"": ""compra.justa"", ""costo_persona"": 60000 }, { ""id"": ""compra.basica"", ""costo_persona"": 90000 } ] },
              ""gastos"": { ""edad_desde"": 0, ""categorias"": [] } } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static Value Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new ExpenseSystem(content)).AdvanceWeeks(world, weeks);

        private static World LivingAlone(ContentDatabase content)
        {
            World world = new LifeSession(content, 1).World;
            world.Player.SetFlag(HousingRules.LivesOnOwnFlag);
            return world;
        }

        [Fact]
        public void SinElegirCompra_NoSeCobraComida()
        {
            ContentDatabase content = Content();
            World world = LivingAlone(content);

            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
            Assert.True(Eval(world, content, "jugador.compra == nada").AsBool("prueba"));
        }

        [Fact]
        public void ElegirCompra_CadaMesPagaLaCompraQueEligio()
        {
            ContentDatabase content = Content();
            World world = LivingAlone(content);

            Run(world, content, "jugador.elegir_compra('compra.justa')");
            Advance(world, content, 1);

            Assert.Equal("compra.justa", Eval(world, content, "jugador.compra").AsText("prueba"));
            Assert.Equal(-60000m, world.Player.Money);
            Assert.Contains(world.Player.LastExpenses, l => l.Id == "compra.justa" && l.Amount == 60000m);
        }

        [Fact]
        public void EnLaCasaDeSuFamilia_LaCompraNoSeCobra()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Run(world, content, "jugador.elegir_compra('compra.basica')");
            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
        }

        [Fact]
        public void ConPareja_LaCompraEsParaDos_YSePagaAMedias()
        {
            ContentDatabase content = Content();
            World world = LivingAlone(content);
            Run(world, content, "jugador.formar_pareja()");
            Run(world, content, "jugador.elegir_compra('compra.basica')");

            Advance(world, content, 1);

            Assert.Equal(-90000m, world.Player.Money);
        }

        [Fact]
        public void ConUnHijo_LaCompraCreceSegunCuantoComeUnNino()
        {
            ContentDatabase content = Content();
            World world = LivingAlone(content);
            Run(world, content, "jugador.tener_hijo()");
            Run(world, content, "jugador.elegir_compra('compra.basica')");

            Assert.Equal(135000, Eval(world, content, "compra('compra.basica')").AsNumber("prueba"));
            Advance(world, content, 1);

            Assert.Equal(-135000m, world.Player.Money);
        }

        [Fact]
        public void UnaCompraQueNoExiste_EsUnError()
        {
            ContentDatabase content = Content();
            World world = LivingAlone(content);

            Assert.Throws<ScriptException>(() => Run(world, content, "jugador.elegir_compra('compra.caviar')"));
        }

        [Fact]
        public void Paquetes_UnEventoQueUsaUnaCompraQueElPaisNoTiene_EsUnError()
        {
            const string events = @"[ { ""id"": ""evento.prueba_compra"", ""probabilidad_anual"": 1, ""opciones"": [
              { ""texto"": ""opcion.prueba"", ""efectos"": [ ""jugador.elegir_compra('compra.caviar')"" ] } ] } ]";
            const string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""supermercado"": { ""compras"": [ { ""id"": ""compra.basica"", ""costo_persona"": 90000 } ] } } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            content.AddEventsJson("pack.prueba", "eventos.json", events);

            ContentLoadException error = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(error.Errors, e => e.Message.Contains("compra.caviar"));
        }

        [Fact]
        public void Chile_LaCanastaBasica_EsLaOficial_YLoJustoCuestaMenosQueLosAntojos()
        {
            GroceriesDefinition rules = RepoPacks.Content.GetCountry("pais.chile").Economy!.Groceries!;

            Assert.Equal(90384m, rules.Basket("compra.basica")!.PerPerson);
            Assert.True(rules.Basket("compra.justa")!.PerPerson < rules.Basket("compra.basica")!.PerPerson);
            Assert.True(rules.Basket("compra.basica")!.PerPerson < rules.Basket("compra.con_antojos")!.PerPerson);
            Assert.Contains(RepoPacks.Content.GetCountry("pais.chile").PriorLife, p => p.GroceryBasket == "compra.basica");
        }

        [Fact]
        public void AlIrseDeLaCasa_ElJugadorDecideSuCompra()
        {
            ContentDatabase content = RepoPacks.Content;
            EventDefinition armar = content.Events.First(e => e.Id == "evento.armar_compra");

            Assert.Equal(3, armar.Options.Count);
            Assert.All(armar.Options, o => Assert.Contains(o.Effects, e => e.Contains("elegir_compra")));
        }
    }
}

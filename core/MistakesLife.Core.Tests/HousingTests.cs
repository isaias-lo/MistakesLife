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
    public class HousingTests
    {
        /// <summary>País de prueba sin inflación: departamento a 300.000 y pieza a 120.000 (con cuentas), contrato de 12 meses.</summary>
        private static ContentDatabase Content(string events = "")
        {
            const string rents = @", ""arriendos"": { ""vivienda.departamento"": 300000, ""vivienda.pieza"": 120000 }";
            string country = CountryTests.CountryJson
                .Replace(@"""nivel_hogar_max"": 40 }", @"""nivel_hogar_max"": 40" + rents + " }")
                .Replace(@"""nivel_hogar_max"": 90 }", @"""nivel_hogar_max"": 90" + rents + " }");
            const string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""vivienda"": { ""plazo_meses"": 12, ""garantia_meses"": 1, ""reajuste_meses"": 6, ""tipos"": [
                { ""id"": ""vivienda.departamento"" }, { ""id"": ""vivienda.pieza"", ""incluye_cuentas"": true } ] },
              ""gastos"": { ""edad_desde"": 0, ""categorias"": [
                { ""id"": ""gasto.prueba.cuentas"", ""monto"": ""50000"", ""condiciones"": ""jugador.tiene('vive_independiente') && !jugador.cuentas_incluidas"" }
              ] } } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", country.TrimEnd().TrimEnd('}') + economy);
            if (events.Length > 0)
            {
                content.AddEventsJson("pack.prueba", "eventos.json", events);
            }

            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static Value Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Advance(World world, ContentDatabase content, int weeks) =>
            new WeekCycle().Register(new EconomySystem(content)).Register(new ExpenseSystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void SinArrendar_NoPagaArriendo()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
            Assert.True(Eval(world, content, "jugador.vivienda == nada").AsBool("prueba"));
        }

        [Fact]
        public void Arrendar_DejaLaGarantia_YCadaMesPagaLoQueDiceElContrato()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.dinero += 1000000");

            Run(world, content, "jugador.arrendar('vivienda.departamento')");
            Assert.Equal(700000m, world.Player.Money);
            Assert.True(world.Player.HasFlag(HousingRules.LivesOnOwnFlag));
            Assert.Equal("vivienda.departamento", Eval(world, content, "jugador.vivienda").AsText("prueba"));
            Assert.Equal(12, Eval(world, content, "jugador.meses_contrato").AsNumber("prueba"));

            Advance(world, content, 1);

            // Arriendo del contrato y las cuentas, que en un departamento no vienen incluidas.
            Assert.Equal(700000m - 300000 - 50000, world.Player.Money);
            Assert.Contains(world.Player.LastExpenses, l => l.Id == "vivienda.departamento" && l.Amount == 300000m);
            Assert.Equal(11, Eval(world, content, "jugador.meses_contrato").AsNumber("prueba"));
        }

        [Fact]
        public void UnaPieza_TraeLasCuentasIncluidas()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.arrendar('vivienda.pieza')");

            Advance(world, content, 1);

            Assert.True(Eval(world, content, "jugador.cuentas_incluidas").AsBool("prueba"));
            Assert.Equal(-120000m - 120000, world.Player.Money);
        }

        [Fact]
        public void CambiarseDeVivienda_DevuelveLaGarantiaAnterior_YCobraLaNueva()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.arrendar('vivienda.departamento')");

            Run(world, content, "jugador.arrendar('vivienda.pieza')");

            Assert.Equal(-120000m, world.Player.Money);
            Assert.Equal("vivienda.pieza", world.Player.Lease!.TypeId);
        }

        [Fact]
        public void DejarElArriendo_DevuelveLaGarantia_YVuelveAVivirConSuFamilia()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.arrendar('vivienda.departamento')");

            Run(world, content, "jugador.dejar_arriendo()");
            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
            Assert.Null(world.Player.Lease);
            Assert.False(world.Player.HasFlag(HousingRules.LivesOnOwnFlag));
        }

        [Fact]
        public void Reajuste_ElArriendoSigueIgualHastaQueTocaYAhiSubeConLaInflacion()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            HousingDefinition rules = content.GetCountry(world.Player.Residence!.CountryId).Economy!.Housing!;
            world.Player.Lease = new Lease("vivienda.departamento", world.Player.Residence.CommuneId, 300000m, 1.0, 12, 0);

            for (int month = 1; month < 6; month++)
            {
                Assert.Equal(300000m, HousingRules.MonthlyRent(content, world.Player, rules, 1.2));
            }

            Assert.Equal(360000m, HousingRules.MonthlyRent(content, world.Player, rules, 1.2));
            Assert.Equal(6, world.Player.Lease.MonthsLeft);
        }

        [Fact]
        public void AlTerminarElPlazo_SigueMesAMes_YRenovarloLoExtiende()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            HousingDefinition rules = content.GetCountry(world.Player.Residence!.CountryId).Economy!.Housing!;
            world.Player.Lease = new Lease("vivienda.departamento", world.Player.Residence.CommuneId, 300000m, 1.0, 1, 0);

            HousingRules.MonthlyRent(content, world.Player, rules, 1.0);
            HousingRules.MonthlyRent(content, world.Player, rules, 1.0);
            Assert.Equal(0, world.Player.Lease.MonthsLeft);

            Run(world, content, "jugador.renovar_arriendo()");

            Assert.Equal(12, world.Player.Lease.MonthsLeft);
        }

        [Fact]
        public void ContratoDeUnGuardadoViejo_TomaElPrecioDeLaComunaElPrimerMes()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.Lease = new Lease("vivienda.pieza", world.Player.Residence!.CommuneId, 0m, 1.0, 12, 0);

            Advance(world, content, 1);

            Assert.Equal(120000m, world.Player.Lease.BaseRent);
            Assert.Equal(-120000m, world.Player.Money);
        }

        [Fact]
        public void ArriendoTipico_SeLeeConLaFuncionArriendo()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Assert.Equal(120000, Eval(world, content, "arriendo('vivienda.pieza')").AsNumber("prueba"));
        }

        [Fact]
        public void Arrendar_UnTipoQueElPaisNoTiene_EsUnErrorDeContenido()
        {
            string events = @"[ { ""id"": ""evento.prueba.castillo"", ""probabilidad_anual"": 1,
                ""efectos"": [ ""jugador.arrendar('vivienda.castillo')"" ] } ]";

            var ex = Assert.Throws<ContentLoadException>(() => Content(events));

            Assert.Contains(ex.Errors, e => e.Message.Contains("vivienda.castillo"));
        }

        [Fact]
        public void Chile_LaPiezaCuestaMenosQueElDepartamento_YElDepartamentoMenosQueLaCasa()
        {
            ContentDatabase content = RepoPacks.Content;
            World world = new LifeSession(content, 1).World;
            double room = Eval(world, content, "arriendo('vivienda.pieza')").AsNumber("prueba");
            double flat = Eval(world, content, "arriendo('vivienda.departamento')").AsNumber("prueba");
            double house = Eval(world, content, "arriendo('vivienda.casa')").AsNumber("prueba");

            Assert.True(room < flat && flat < house, $"pieza {room}, departamento {flat}, casa {house}.");
        }

        [Fact]
        public void Legado_UnHijoDe28Anos_YaArriendaUnDepartamento_SinPagarGarantia()
        {
            HousingDefinition rules = RepoPacks.Content.GetCountry("pais.chile").Economy!.Housing!;

            Assert.NotNull(rules.Type("vivienda.departamento"));
            Assert.Contains(RepoPacks.Content.GetCountry("pais.chile").PriorLife, p => p.Housing == "vivienda.departamento");
        }
    }
}

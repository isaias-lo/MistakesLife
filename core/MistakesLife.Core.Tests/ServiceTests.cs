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
    public class ServiceTests
    {
        /// <summary>
        /// País de prueba sin inflación: un servicio de celular (solo con la marca 'con_celular'), uno de
        /// internet compartido, y un jardín y un colegio que se pagan por cada hijo con esa edad.
        /// </summary>
        private static ContentDatabase Content(string events = "")
        {
            const string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""servicios"": [
                { ""id"": ""servicio.celular"", ""condiciones"": ""jugador.tiene('con_celular')"", ""opciones"": [
                  { ""id"": ""servicio.celular.prepago"", ""precio"": 6000 }, { ""id"": ""servicio.celular.plan"", ""precio"": 13000 } ] },
                { ""id"": ""servicio.internet"", ""compartido"": true, ""opciones"": [ { ""id"": ""servicio.internet.fibra"", ""precio"": 20000 } ] },
                { ""id"": ""servicio.jardin"", ""cantidad"": ""jugador.hijos_en_casa_entre(0, 3)"", ""opciones"": [ { ""id"": ""servicio.jardin.particular"", ""precio"": 50000 } ] },
                { ""id"": ""servicio.colegio"", ""cantidad"": ""jugador.hijos_en_casa_entre(4, 17)"", ""opciones"": [ { ""id"": ""servicio.colegio.particular"", ""precio"": 90000 } ] } ],
              ""gastos"": { ""edad_desde"": 0, ""categorias"": [] } } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
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
        public void SinContratar_NoSePagaNada()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.SetFlag("con_celular");

            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
            Assert.True(Eval(world, content, "jugador.servicio('servicio.celular') == nada").AsBool("prueba"));
        }

        [Fact]
        public void Contratar_CadaMesPagaLoQueContrato()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.SetFlag("con_celular");

            Run(world, content, "jugador.contratar('servicio.celular.plan')");
            Advance(world, content, 1);

            Assert.Equal("servicio.celular.plan", Eval(world, content, "jugador.servicio('servicio.celular')").AsText("prueba"));
            Assert.Equal(-13000m, world.Player.Money);
            Assert.Contains(world.Player.LastExpenses, l => l.Id == "servicio.celular.plan" && l.Amount == 13000m);
            Assert.Equal(13000, Eval(world, content, "tarifa('servicio.celular.plan')").AsNumber("prueba"));
        }

        [Fact]
        public void ContratarOtraOpcion_CambiaLaQueTenia()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.SetFlag("con_celular");
            Run(world, content, "jugador.contratar('servicio.celular.plan')");

            Run(world, content, "jugador.contratar('servicio.celular.prepago')");
            Advance(world, content, 1);

            Assert.Equal(-6000m, world.Player.Money);
        }

        [Fact]
        public void Cancelar_DejaDeCobrarse()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            world.Player.SetFlag("con_celular");
            Run(world, content, "jugador.contratar('servicio.celular.plan')");

            Run(world, content, "jugador.cancelar_servicio('servicio.celular')");
            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
        }

        [Fact]
        public void SiNoSeCumpleLaCondicionDelServicio_NoSeCobra()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.contratar('servicio.celular.plan')");

            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
        }

        [Fact]
        public void UnServicioCompartido_SePagaAMediasConPareja()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.formar_pareja()");
            Run(world, content, "jugador.contratar('servicio.internet.fibra')");

            Advance(world, content, 1);

            Assert.Equal(-10000m, world.Player.Money);
        }

        [Fact]
        public void UnServicioConCantidad_SePagaPorCadaHijoDeEsaEdad()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.contratar('servicio.jardin.particular')");
            Run(world, content, "jugador.contratar('servicio.colegio.particular')");
            Run(world, content, "jugador.tener_hijo()");
            Run(world, content, "jugador.tener_hijo()");

            Advance(world, content, 1);

            // Dos guaguas: se paga el jardín dos veces y el colegio ninguna (todavía no tienen edad).
            Assert.Equal(2, Eval(world, content, "jugador.hijos_en_casa_entre(0, 3)").AsNumber("prueba"));
            Assert.Equal(0, Eval(world, content, "jugador.hijos_en_casa_entre(4, 17)").AsNumber("prueba"));
            Assert.Equal(-100000m, world.Player.Money);
            Assert.DoesNotContain(world.Player.LastExpenses, l => l.Id == "servicio.colegio.particular");
        }

        [Fact]
        public void UnServicioConCantidad_SinHijosDeEsaEdad_NoSeCobra()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.contratar('servicio.jardin.particular')");

            Advance(world, content, 1);

            Assert.Equal(0m, world.Player.Money);
        }

        [Fact]
        public void ContratarAlgoQueNoExiste_EsUnError()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Assert.Throws<ScriptException>(() => Run(world, content, "jugador.contratar('servicio.celular.satelital')"));
        }

        [Fact]
        public void Paquetes_UnEventoQueContrataAlgoQueElPaisNoTiene_EsUnError()
        {
            const string events = @"[ { ""id"": ""evento.prueba_servicio"", ""probabilidad_anual"": 1, ""opciones"": [
              { ""texto"": ""opcion.prueba"", ""efectos"": [ ""jugador.contratar('servicio.celular.satelital')"" ] } ] } ]";

            ContentLoadException error = Assert.Throws<ContentLoadException>(() => Content(events));
            Assert.Contains(error.Errors, e => e.Message.Contains("servicio.celular.satelital"));
        }

        [Fact]
        public void Chile_ElPlanDelCelular_YaNoEsUnGastoFijo_YElPrepagoEsLoMasBarato()
        {
            CountryDefinition chile = RepoPacks.Content.GetCountry("pais.chile");
            ServiceDefinition phone = chile.Economy!.Services.Single(s => s.Id == "servicio.celular");

            Assert.DoesNotContain(chile.Economy.Expenses!.Categories, e => e.Id == "gasto.chile.celular");
            Assert.True(phone.Option("servicio.celular.prepago")!.Price < phone.Option("servicio.celular.plan")!.Price);
            Assert.True(phone.Option("servicio.celular.plan")!.Price < phone.Option("servicio.celular.plan_ilimitado")!.Price);
        }

        [Fact]
        public void SinCelular_ElPrepagoNoSeCobra_PeroUnPlanSigueCorriendo()
        {
            ContentDatabase content = RepoPacks.Content;
            World world = new LifeSession(content, 1).World;

            Run(world, content, "jugador.contratar('servicio.celular.prepago')");
            Assert.False(content.GetCountry("pais.chile").Economy!.Services.Single(x => x.Id == "servicio.celular").CompiledConditions!.IsTrue(new EventScope(world, content, world.Player, null, readOnly: true)));

            Run(world, content, "jugador.contratar('servicio.celular.plan')");
            Assert.True(content.GetCountry("pais.chile").Economy!.Services.Single(x => x.Id == "servicio.celular").CompiledConditions!.IsTrue(new EventScope(world, content, world.Player, null, readOnly: true)));
        }
        [Theory]
        [InlineData("pais.chile")]
        [InlineData("pais.argentina")]
        public void Transporte_YaNoEsUnGastoFijo_ElJugadorEligeComoSeMueve(string countryId)
        {
            CountryDefinition country = RepoPacks.Content.GetCountry(countryId);
            ServiceDefinition transport = country.Economy!.Services.Single(s => s.Id == "servicio.transporte");

            Assert.DoesNotContain(country.Economy.Expenses!.Categories, e => e.Id.EndsWith(".transporte"));
            Assert.Equal(0m, transport.Option("servicio.transporte.caminando")!.Price);
            Assert.True(transport.Option("servicio.transporte.bus")!.Price < transport.Option("servicio.transporte.bus_y_metro")!.Price);
        }

        [Fact]
        public void Chile_ElPasajeDelMes_EsElPasajeRealPor44Viajes()
        {
            ServiceDefinition transport = RepoPacks.Content.GetCountry("pais.chile").Economy!.Services.Single(s => s.Id == "servicio.transporte");

            Assert.Equal(795m * 44, transport.Option("servicio.transporte.bus")!.Price);
            Assert.Equal(895m * 44, transport.Option("servicio.transporte.bus_y_metro")!.Price);
        }
        [Theory]
        [InlineData("pais.chile")]
        [InlineData("pais.argentina")]
        public void Internet_LoContrataElJugador_SeComparteConLaPareja_YLaFibraRapidaCuestaMas(string countryId)
        {
            ServiceDefinition internet = RepoPacks.Content.GetCountry(countryId).Economy!.Services.Single(s => s.Id == "servicio.internet");

            Assert.True(internet.Shared);
            Assert.True(internet.Option("servicio.internet.basico")!.Price < internet.Option("servicio.internet.rapido")!.Price);
        }
        [Theory]
        [InlineData("pais.chile")]
        [InlineData("pais.argentina")]
        public void Cuentas_DependenDeComoUsaLaCasaElJugador_YYaNoSonUnGastoFijo(string countryId)
        {
            CountryDefinition country = RepoPacks.Content.GetCountry(countryId);
            ServiceDefinition bills = country.Economy!.Services.Single(s => s.Id == "servicio.cuentas");

            Assert.True(bills.Shared);
            Assert.DoesNotContain(country.Economy.Expenses!.Categories, e => e.Id.EndsWith(".cuentas") || e.Id.EndsWith(".servicios"));
            Assert.True(bills.Option("servicio.cuentas.cuidando")!.Price < bills.Option("servicio.cuentas.normal")!.Price);
            Assert.True(bills.Option("servicio.cuentas.normal")!.Price < bills.Option("servicio.cuentas.sin_cuidar")!.Price);
        }

        [Fact]
        public void EnUnaPiezaConCuentasIncluidas_LasCuentasNoSeCobran()
        {
            ContentDatabase content = RepoPacks.Content;
            World world = new LifeSession(content, 1).World;
            world.Player.SetFlag("vive_independiente");
            ServiceDefinition bills = content.GetCountry("pais.chile").Economy!.Services.Single(s => s.Id == "servicio.cuentas");
            var scope = new EventScope(world, content, world.Player, null, readOnly: true);

            Run(world, content, "jugador.arrendar('vivienda.departamento')");
            Assert.True(bills.CompiledConditions!.IsTrue(scope));

            Run(world, content, "jugador.arrendar('vivienda.pieza')");
            Assert.False(bills.CompiledConditions!.IsTrue(scope));
        }
        [Fact]
        public void Chile_GastosPersonales_LosEligeElJugador_ConLoQueGastanLosHogaresSegunLaEpf()
        {
            CountryDefinition chile = RepoPacks.Content.GetCountry("pais.chile");
            ServiceDefinition personal = chile.Economy!.Services.Single(s => s.Id == "servicio.personales");

            Assert.DoesNotContain(chile.Economy.Expenses!.Categories, e => e.Id == "gasto.chile.personales");
            Assert.False(personal.Shared);
            Assert.True(personal.Option("servicio.personales.austero")!.Price < personal.Option("servicio.personales.normal")!.Price);
            Assert.True(personal.Option("servicio.personales.normal")!.Price < personal.Option("servicio.personales.gustos")!.Price);
        }

        [Theory]
        [InlineData("pais.chile")]
        [InlineData("pais.argentina")]
        public void Hijos_ElJardinYElColegio_LosEligeElJugador_YYaNoHayUnGastoFijoPorHijo(string countryId)
        {
            CountryDefinition country = RepoPacks.Content.GetCountry(countryId);
            ServiceDefinition daycare = country.Economy!.Services.Single(s => s.Id == "servicio.jardin_hijos");
            ServiceDefinition school = country.Economy.Services.Single(s => s.Id == "servicio.colegio_hijos");

            Assert.DoesNotContain(country.Economy.Expenses!.Categories, e => e.Id.EndsWith(".hijos"));
            Assert.True(daycare.Shared && school.Shared);
            Assert.Equal(0m, daycare.Option("servicio.jardin_hijos.publico")!.Price);
            Assert.True(daycare.Option("servicio.jardin_hijos.particular")!.Price > 0);
            Assert.Equal(0m, school.Option("servicio.colegio_hijos.gratuito")!.Price);
            Assert.True(school.Option("servicio.colegio_hijos.copago")!.Price < school.Option("servicio.colegio_hijos.particular")!.Price);
        }
    }
}

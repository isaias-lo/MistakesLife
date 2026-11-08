using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Varios países a la vez: los eventos de un país le pasan solo a quien vive ahí,
    /// y cada país puede decir los mismos textos a su manera (variantes de idioma).
    /// </summary>
    public class CountryScopeTests
    {
        private static string Country(string id, string extra = "") =>
            CountryTests.CountryJson.Replace("pais.prueba", id).Replace("ciudad.prueba", "ciudad." + id).Replace("comuna.", "comuna." + id + ".")
                .Replace("etapa.prueba.", "etapa." + id + ".").TrimEnd().TrimEnd('}') + extra + "}";

        private const string EventJson = @"[
          { ""id"": ""evento.solo_aqui"", ""peso"": 1000, ""unico"": true, ""efectos"": [""jugador.marcar('paso_aqui')""] }
        ]";

        private static ContentDatabase TwoCountries()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.aqui", "country.json", Country("pais.aqui"));
            content.AddCountryJson("pack.alla", "country.json", Country("pais.alla"));
            content.AddEventsJson("pack.aqui", "events/aqui.json", EventJson);
            return content.Build();
        }

        private static World LifeIn(ContentDatabase content, string countryId)
        {
            for (ulong seed = 1; seed < 200; seed++)
            {
                World world = WorldFactory.CreateNewLife(seed, content);
                if (world.Player.Residence!.CountryId == countryId)
                {
                    return world;
                }
            }

            throw new Xunit.Sdk.XunitException("Ninguna vida nació en " + countryId);
        }

        private static void Play(ContentDatabase content, World world, int weeks)
        {
            var cycle = new WeekCycle().Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 1.0 }));
            cycle.AdvanceWeeks(world, weeks);
        }

        [Fact]
        public void EventoDelPaqueteDeUnPais_SoloLePasaAQuienViveAhi()
        {
            ContentDatabase content = TwoCountries();
            World here = LifeIn(content, "pais.aqui");
            World there = LifeIn(content, "pais.alla");

            Play(content, here, 20);
            Play(content, there, 20);

            Assert.True(here.Player.HasFlag("paso_aqui"));
            Assert.False(there.Player.HasFlag("paso_aqui"));
            Assert.Equal("pais.aqui", content.GetEvent("evento.solo_aqui").CountryId);
        }

        [Fact]
        public void EventoDelPaqueteBase_LePuedePasarAQuienSea()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.aqui", "country.json", Country("pais.aqui"));
            content.AddEventsJson("pack.base", "events/base.json", EventJson);
            content.Build();

            Assert.Null(content.GetEvent("evento.solo_aqui").CountryId);
        }

        [Fact]
        public void Texto_ConVarianteDelPais_UsaLaVarianteYSiFaltaElGeneral()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.aqui", "country.json", Country("pais.aqui", @", ""variantes_idioma"": { ""es"": ""es-AQ"" }"));
            content.AddTextsJson("pack.base", "lang/es.json", "es", @"{ ""trabajo"": ""trabajo"", ""casa"": ""casa"" }");
            content.AddTextsJson("pack.aqui", "lang/es-AQ.json", "es-AQ", @"{ ""trabajo"": ""pega"" }");

            // Sin Build: armar exigiría todos los textos del núcleo, y aquí solo importa cómo se buscan.

            Assert.Equal("es-AQ", content.LanguageFor("pais.aqui"));
            Assert.Equal("es", content.LanguageFor("pais.otro"));
            Assert.Equal("pega", content.Text("trabajo", "es-AQ"));
            Assert.Equal("casa", content.Text("casa", "es-AQ"));
            Assert.Equal("trabajo", content.Text("trabajo"));
        }

        [Fact]
        public void Variante_ConUnTextoQueNoExisteEnElIdiomaGeneral_EsUnError()
        {
            var content = new ContentDatabase();
            content.AddTextsJson("pack.base", "lang/es.json", "es", @"{ ""trabajo"": ""trabajo"" }");
            content.AddTextsJson("pack.aqui", "lang/es-AQ.json", "es-AQ", @"{ ""inventado"": ""algo"" }");

            var error = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(error.Errors, e => e.Message.Contains("'inventado' no existe en 'es'"));
        }

        [Fact]
        public void Partida_EnChile_HablaComoEnChile()
        {
            var session = new LifeSession(RepoPacks.Content, 1);

            Assert.Equal("pais.chile", session.World.Player.Residence!.CountryId);
            Assert.Contains("pega", session.Text("evento.rival_trabajo"));
            Assert.DoesNotContain("pega", RepoPacks.Content.Text("evento.rival_trabajo"));
        }

        [Fact]
        public void PaqueteBase_NoDependeDeChile()
        {
            // Los eventos comunes (amigos, pareja, salud, trabajo) viven en el paquete base
            // y le pueden pasar a quien sea; los de Chile (PAES, gratuidad) solo en Chile.
            Assert.Null(RepoPacks.Content.GetEvent("evento.rival_trabajo").CountryId);
            Assert.Null(RepoPacks.Content.GetEvent("evento.jubilacion").CountryId);
            Assert.Equal("pais.chile", RepoPacks.Content.GetEvent("evento.prueba_paes").CountryId);
            Assert.All(RepoPacks.Content.Events.Where(e => e.PackId == "pack.base"), e => Assert.Null(e.CountryId));
        }
    }
}

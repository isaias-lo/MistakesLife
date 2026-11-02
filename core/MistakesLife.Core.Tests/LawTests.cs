using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class LawTests
    {
        private const string Laws = @", ""leyes"": { ""ley.matrimonio_igualitario"": false, ""ley.edad_voto"": 16, ""ley.aborto"": ""prohibido"" } }";

        private static ContentDatabase Content(string laws = Laws, string? eventsJson = null)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + laws);
            if (eventsJson != null)
            {
                content.AddEventsJson("pack.prueba", "events/prueba.json", eventsJson);
            }

            return content.Build();
        }

        private static bool Check(World world, ContentDatabase content, string condition) =>
            Expression.Parse(condition).IsTrue(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        [Fact]
        public void Ley_LeeElValorDelPaisDondeViveElJugador()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Assert.True(Check(world, content, "!ley('ley.matrimonio_igualitario')"));
            Assert.True(Check(world, content, "ley('ley.edad_voto') == 16"));
            Assert.True(Check(world, content, "ley('ley.aborto') == 'prohibido'"));
        }

        [Fact]
        public void CambiarLey_DuranteLaPartida_CambiaLoQueLeenLosEventos()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;

            Run(world, content, "cambiar_ley('ley.matrimonio_igualitario', true)");

            Assert.True(Check(world, content, "ley('ley.matrimonio_igualitario')"));
            Assert.False(content.GetCountry("pais.prueba").TryGetLaw("ley.matrimonio_igualitario", out Value original) && original.AsBool("prueba"));
        }

        [Fact]
        public void EventoQueUsaUnaLeyQueElPaisNoDefine_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content(eventsJson: @"[
              { ""id"": ""evento.votar"", ""condiciones"": ""jugador.edad >= ley('ley.no_existe')"" }
            ]"));

            Assert.Contains(ex.Errors, e => e.Message.Contains("no define esa ley"));
        }

        [Fact]
        public void CambiarLeyEnUnaCondicion_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content(eventsJson: @"[
              { ""id"": ""evento.votar"", ""condiciones"": ""cambiar_ley('ley.edad_voto', 18) == nada"" }
            ]"));

            Assert.Contains(ex.Errors, e => e.Message.Contains("solo puede usarse en efectos"));
        }

        [Fact]
        public void LeyConIdMalEscritoOValorQueNoEsSiNoNumeroNiTexto_EsUnErrorDeContenido()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content(@", ""leyes"": { ""Matrimonio"": true, ""ley.lista"": [1, 2] } }"));

            Assert.Contains(ex.Errors, e => e.Message.Contains("'ley.'"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("el valor de una ley"));
        }

        [Fact]
        public void Chile_DefineSusLeyes_YLosEventosLasUsan()
        {
            ContentDatabase content = RepoPacks.Content;
            World world = new LifeSession(content, 1).World;

            Assert.True(Check(world, content, "ley('ley.matrimonio_igualitario') && ley('ley.edad_voto') == 18 && ley('ley.aborto') == 'tres_causales'"));
            Assert.Contains("ley('ley.edad_voto')", content.GetEvent("evento.primera_votacion").Conditions);
        }
    }
}

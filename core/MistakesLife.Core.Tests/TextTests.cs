using MistakesLife.Core.Content;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class TextTests
    {
        [Fact]
        public void TextoFaltante_SeDetectaAlCargar()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events.json", @"[{ ""id"": ""evento.a"", ""opciones"": [ { ""texto"": ""opcion.b"" } ] }]");
            content.AddTextsJson("pack.prueba", "lang/es.json", "es", @"{ ""evento.a"": ""Algo pasa."" }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("opcion.b"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("diario.nacimiento"));
        }

        [Fact]
        public void Texto_UsaElIdiomaBaseSiFaltaLaTraduccion()
        {
            var content = new ContentDatabase();
            content.AddTextsJson("pack.prueba", "lang/es.json", "es", @"{ ""hola"": ""Hola"", ""chao"": ""Chao"" }");
            content.AddTextsJson("pack.prueba", "lang/en.json", "en", @"{ ""hola"": ""Hello"" }");

            Assert.Equal("Hello", content.Text("hola", "en"));
            Assert.Equal("Chao", content.Text("chao", "en"));
            Assert.Equal("no.existe", content.Text("no.existe", "en"));
        }

        [Fact]
        public void TextoRepetidoOVacio_SeDetecta()
        {
            var content = new ContentDatabase();
            content.AddTextsJson("pack.a", "a/es.json", "es", @"{ ""x"": ""uno"", ""y"": """" }");
            content.AddTextsJson("pack.b", "b/es.json", "es", @"{ ""x"": ""dos"" }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("ya existe"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("vacío"));
        }
    }
}

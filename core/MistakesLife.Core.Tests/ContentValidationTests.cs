using System.Linq;
using MistakesLife.Core.Content;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class ContentValidationTests
    {
        private static ContentLoadException Fails(string seeds, string events)
        {
            var content = new ContentDatabase();
            content.AddSeedsJson("pack.prueba", "seeds.json", seeds);
            content.AddEventsJson("pack.prueba", "events.json", events);
            return Assert.Throws<ContentLoadException>(() => content.Build());
        }

        [Fact]
        public void SemillaInexistente_SeDetecta()
        {
            var ex = Fails("[]", @"[{ ""id"": ""evento.a"", ""condiciones"": ""semilla('semilla.no_existe').activa"" }]");
            Assert.Contains(ex.Errors, e => e.Message.Contains("semilla.no_existe"));
        }

        [Fact]
        public void CondicionMalEscrita_SeDetectaConSuId()
        {
            var ex = Fails("[]", @"[{ ""id"": ""evento.a"", ""condiciones"": ""jugador.edad >="" }]");
            Assert.Contains(ex.Errors, e => e.Source == "evento evento.a");
        }

        [Fact]
        public void NombreDesconocido_SeDetecta()
        {
            var ex = Fails("[]", @"[{ ""id"": ""evento.a"", ""condiciones"": ""jugadr.edad > 3"" }]");
            Assert.Contains(ex.Errors, e => e.Message.Contains("jugadr"));
        }

        [Fact]
        public void FuncionDeEfectoEnUnaCondicion_SeDetecta()
        {
            var ex = Fails(@"[{ ""id"": ""semilla.x"", ""tipo"": ""positiva"" }]", @"[
              { ""id"": ""evento.a"", ""condiciones"": ""plantar('semilla.x') == 1"" },
              { ""id"": ""evento.b"", ""semilla"": ""semilla.x"" }
            ]");
            Assert.Contains(ex.Errors, e => e.Message.Contains("solo puede usarse en efectos"));
        }

        [Fact]
        public void IdsRepetidosOMalFormados_SeDetectan()
        {
            var ex = Fails("[]", @"[{ ""id"": ""evento.a"" }, { ""id"": ""evento.a"" }, { ""id"": ""Evento.B"" }]");
            Assert.Contains(ex.Errors, e => e.Message.Contains("repetido"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("minúsculas"));
        }

        [Fact]
        public void SemillaSinEventos_SeDetecta()
        {
            var ex = Fails(@"[{ ""id"": ""semilla.huerfana"", ""tipo"": ""positiva"" }]", "[]");
            Assert.Contains(ex.Errors, e => e.Message.Contains("ningún evento"));
        }

        [Fact]
        public void CampoDesconocidoYTipoInvalido_SeDetectan()
        {
            var ex = Fails(@"[{ ""id"": ""semilla.x"", ""tipo"": ""rara"" }]", @"[{ ""id"": ""evento.a"", ""semilla"": ""semilla.x"", ""pesso"": 3 }]");
            Assert.Contains(ex.Errors, e => e.Message.Contains("JSON no válido"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("rara"));
        }

        [Fact]
        public void TodosLosErrores_SeReportanJuntos()
        {
            var ex = Fails("[]", @"[{ ""id"": ""evento.a"", ""condiciones"": ""x >"" }, { ""id"": ""evento.b"", ""peso"": 0 }]");
            Assert.True(ex.Errors.Count >= 2);
        }
    }
}

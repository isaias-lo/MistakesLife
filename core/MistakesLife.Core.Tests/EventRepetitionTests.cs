using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Eventos que se repiten: "veces_maximas" pone un tope en la vida y
    /// "variantes" cuenta la escena de otra forma cada vez que vuelve a pasar.
    /// </summary>
    public class EventRepetitionTests
    {
        private static ContentDatabase Content(string events)
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        private static (World World, WeekCycle Cycle) Setup(ContentDatabase content)
        {
            var world = WorldFactory.CreateNewLife(7);
            var cycle = new WeekCycle().Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 1.0 }));
            return (world, cycle);
        }

        [Fact]
        public void VecesMaximas_ElEventoNoPasaMasDeEsasVecesEnLaVida()
        {
            var (world, cycle) = Setup(Content(@"[ { ""id"": ""evento.comun"", ""veces_maximas"": 3 } ]"));

            cycle.AdvanceWeeks(world, 100);

            Assert.Equal(3, world.TimesHappened("evento.comun", world.Player.Id));
        }

        [Fact]
        public void VecesMaximas_TambienValeParaEventosConProbabilidadAnual()
        {
            var (world, cycle) = Setup(Content(@"[ { ""id"": ""evento.anual"", ""probabilidad_anual"": 1, ""veces_maximas"": 2 } ]"));

            cycle.AdvanceWeeks(world, 300);

            Assert.Equal(2, world.EventHistory.Count(r => r.EventId == "evento.anual"));
        }

        [Fact]
        public void Variantes_CadaVezQueVuelveSeCuentaDistinto_YLuegoSeRepitenEnOrden()
        {
            EventDefinition ev = Content(@"[ { ""id"": ""evento.temblor"", ""variantes"": [""evento.temblor.2"", ""evento.temblor.3""] } ]")
                .GetEvent("evento.temblor");

            string[] keys = Enumerable.Range(0, 5).Select(ev.TextKeyFor).ToArray();

            Assert.Equal(new[] { "evento.temblor", "evento.temblor.2", "evento.temblor.3", "evento.temblor", "evento.temblor.2" }, keys);
        }

        [Fact]
        public void SinVariantes_SiempreSeUsaElTextoPrincipal()
        {
            EventDefinition ev = Content(@"[ { ""id"": ""evento.simple"", ""texto"": ""evento.simple.texto"" } ]").GetEvent("evento.simple");

            Assert.Equal("evento.simple.texto", ev.TextKeyFor(4));
        }

        [Fact]
        public void TextosDeLasVariantes_SeExigenComoCualquierTexto()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.temblor"", ""variantes"": [""evento.temblor.2""] } ]");

            Assert.Contains(content.UsedTextKeys(), k => k.Key == "evento.temblor.2");
        }

        [Fact]
        public void VariantesOVecesMaximasMalEscritas_SonErroresDelPaquete()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[
              { ""id"": ""evento.cero"", ""veces_maximas"": 0 },
              { ""id"": ""evento.repetida"", ""variantes"": [""evento.repetida""] }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.cero") && e.Message.Contains("veces_maximas"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.repetida") && e.Message.Contains("variantes"));
        }
    }
}

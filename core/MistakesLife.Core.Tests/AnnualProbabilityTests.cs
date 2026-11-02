using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class AnnualProbabilityTests
    {
        private static ContentDatabase Content(string events)
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        [Fact]
        public void ProbabilidadAnual_SeCumpleEnPromedioConLosAnios()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.robo"", ""probabilidad_anual"": 0.3 } ]");
            var world = WorldFactory.CreateNewLife(11);
            var cycle = new WeekCycle().Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 0 }));

            const int years = 400;
            cycle.AdvanceWeeks(world, years * GameDate.WeeksPerYear);

            // Años en que ocurrió al menos una vez: cerca del 30 %.
            int yearsWithEvent = world.EventHistory.Select(r => r.Date.WeeksSince(new GameDate(0)) / GameDate.WeeksPerYear).Distinct().Count();
            double share = (double)yearsWithEvent / years;
            Assert.InRange(share, 0.22, 0.38);
        }

        [Fact]
        public void ProbabilidadAnual_NoDependeDeLaProbabilidadSemanalGeneral()
        {
            // Aunque los eventos por peso estén apagados, el evento con probabilidad anual sigue ocurriendo.
            ContentDatabase content = Content(@"[
              { ""id"": ""evento.robo"", ""probabilidad_anual"": 0.9 },
              { ""id"": ""evento.comun"", ""peso"": 1000 } ]");
            var world = WorldFactory.CreateNewLife(11);
            new WeekCycle().Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 0 }))
                .AdvanceWeeks(world, 10 * GameDate.WeeksPerYear);

            Assert.Contains(world.EventHistory, r => r.EventId == "evento.robo");
            Assert.DoesNotContain(world.EventHistory, r => r.EventId == "evento.comun");
        }

        [Fact]
        public void ProbabilidadAnual_FueraDeRangoOConSemilla_SeRechaza()
        {
            var content = new ContentDatabase();
            content.AddSeedsJson("pack.prueba", "seeds/s.json", @"[ { ""id"": ""semilla.x"", ""tipo"": ""negativa"" } ]");
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[
              { ""id"": ""evento.mal"", ""probabilidad_anual"": 1.5 },
              { ""id"": ""evento.semilla"", ""semilla"": ""semilla.x"", ""probabilidad_anual"": 0.5 } ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.mal") && e.Message.Contains("probabilidad_anual"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.semilla") && e.Message.Contains("probabilidad_anual"));
        }
    }
}

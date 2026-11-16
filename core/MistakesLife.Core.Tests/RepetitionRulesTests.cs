using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Analysis;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Regla de repeticiones (R7): un evento no se vuelve a leer igual con la misma persona, y
    /// "por_persona" cuenta el tope y las variantes con cada persona.
    /// </summary>
    public class RepetitionRulesTests
    {
        private static ContentDatabase Content(string events)
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        private static readonly GameDate Day = new GameDate(100);

        [Fact]
        public void MismoTextoSinPersona_CuentaComoRepeticionSinRazon()
        {
            ContentDatabase content = Content(@"[ { ""id"": ""evento.temblor"" } ]");
            var lived = new List<EventRecord> { new EventRecord(Day, "evento.temblor", 1, null), new EventRecord(Day, "evento.temblor", 1, null) };

            Dictionary<string, int> repeats = RepetitionRules.RepeatsWithoutReason(lived, content);

            Assert.Equal(1, repeats["evento.temblor"]);
        }

        [Fact]
        public void OtraPersonaUOtraVariante_NoEsRepeticion()
        {
            ContentDatabase content = Content(@"[
              { ""id"": ""evento.lluvia"", ""variantes"": [""evento.lluvia.2""] },
              { ""id"": ""evento.junta"", ""con"": ""amigo"" }
            ]");
            var lived = new List<EventRecord>
            {
                new EventRecord(Day, "evento.lluvia", 1, null),
                new EventRecord(Day, "evento.lluvia", 1, null),
                new EventRecord(Day, "evento.junta", 1, null, personId: 5),
                new EventRecord(Day, "evento.junta", 1, null, personId: 6),
            };

            Assert.Empty(RepetitionRules.RepeatsWithoutReason(lived, content));
        }

        [Fact]
        public void PorPersona_ElTopeEsConCadaPersona_YLaVivesUnaVezConCadaAmigo()
        {
            var world = new LifeSession(RepoPacks.Content, 3).World;
            new WeekCycle().AdvanceWeeks(world, 25 * GameDate.WeeksPerYear);
            FamilyFactory.CreateAcquaintance(world, RepoPacks.Content, world.Player, RelationshipBond.Friend);
            FamilyFactory.CreateAcquaintance(world, RepoPacks.Content, world.Player, RelationshipBond.Friend);
            int friends = RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend).Count;
            ContentDatabase content = Content(@"[ { ""id"": ""evento.junta"", ""con"": ""amigo"", ""por_persona"": true, ""veces_maximas"": 1 } ]");
            var cycle = new WeekCycle().Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 1.0 }));

            cycle.AdvanceWeeks(world, 60);

            var lived = world.EventHistory.Where(r => r.EventId == "evento.junta").ToList();
            Assert.True(friends >= 2);
            Assert.Equal(friends, lived.Count);
            Assert.Equal(friends, lived.Select(r => r.PersonId).Distinct().Count());
            EventDefinition ev = content.GetEvent("evento.junta");
            Assert.All(lived, r => Assert.Equal(1, world.TimesHappened(ev, world.PlayerId, r.PersonId)));
        }

        [Fact]
        public void EnfermarseYLaUrgencia_TienenUnTextoDistintoParaCadaVez()
        {
            foreach (string id in new[] { "evento.chile.enfermarse", "evento.chile.urgencia" })
            {
                EventDefinition ev = RepoPacks.Content.GetEvent(id);
                Assert.NotNull(ev.MaxTimes);
                Assert.True(ev.AllTextKeys.Distinct().Count() >= ev.MaxTimes!.Value, id + " necesita un texto por cada vez que puede pasar");
            }
        }

        [Fact]
        public void PorPersonaSinCon_EsUnErrorDelPaquete()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[ { ""id"": ""evento.solo"", ""por_persona"": true } ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.solo") && e.Message.Contains("por_persona"));
        }

        [Fact]
        public void MomentosConTuGente_TienenConParaNombrarALaPersona()
        {
            string[] ids =
            {
                "evento.dejar_de_ser_colegas", "evento.juntarse_con_amigos", "evento.pelea_amigo", "evento.amigo_pide_plata", "evento.rival_se_calma",
                "evento.discusion_adolescente", "evento.visitar_mama", "evento.visitar_papa", "evento.discusion_plata_pareja", "evento.detalle_pareja",
                "evento.crisis_pareja", "evento.conversar_hijos", "evento.amigo_pide_consejo", "evento.cumpleanos_sorpresa",
            };

            foreach (string id in ids)
            {
                EventDefinition ev = RepoPacks.Content.GetEvent(id);
                Assert.True(ev.With != null, id + " necesita con");
                Assert.Contains("{persona}", RepoPacks.Content.Text(ev.ResolvedTextKey, "es"));
            }
        }
    }
}

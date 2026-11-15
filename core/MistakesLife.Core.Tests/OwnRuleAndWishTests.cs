using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R2c: tus reglas ("si hay prueba, estudio") y lo que quiero.</summary>
    public class OwnRuleAndWishTests
    {
        private const string TestEvents = @"[
          { ""id"": ""evento.prueba_a"", ""regla"": ""regla.prueba"", ""probabilidad_anual"": 1, ""enfriamiento_semanas"": 2, ""opciones"": [
            { ""texto"": ""opcion.a.estudiar"", ""regla"": ""estudiar"", ""efectos"": [ ""jugador.inteligencia += 1"" ] },
            { ""texto"": ""opcion.a.copiar"", ""regla"": ""copiar"" } ] },
          { ""id"": ""evento.prueba_b"", ""regla"": ""regla.prueba"", ""probabilidad_anual"": 1, ""enfriamiento_semanas"": 2, ""opciones"": [
            { ""texto"": ""opcion.b.copiar"", ""regla"": ""copiar"" },
            { ""texto"": ""opcion.b.estudiar"", ""regla"": ""estudiar"", ""efectos"": [ ""jugador.inteligencia += 1"" ] } ] },
          { ""id"": ""evento.clave"", ""dia_clave"": true, ""opciones"": [ { ""texto"": ""opcion.c.1"" }, { ""texto"": ""opcion.c.2"" } ] }
        ]";

        private static ContentDatabase Content(string events = TestEvents)
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        private static WeekCycle Events(ContentDatabase content) =>
            new WeekCycle().Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 0 }));

        [Fact]
        public void Regla_HechaEnUnaPrueba_ElijeSolaEnOtraPruebaDelGrupoSinDetenerElTiempo()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(3);
            world.Player.OwnRules.Add(new OwnRule("regla.prueba", "estudiar"));
            int smarts = world.Player.Stats.Smarts;

            Events(content).AdvanceWeeks(world, 20, out bool stopped);

            Assert.False(stopped);
            Assert.Empty(world.PendingEvents);
            Assert.Contains(world.EventHistory, r => r.EventId == "evento.prueba_b" && r.ChosenOption == 1);
            Assert.Contains(world.EventHistory, r => r.EventId == "evento.prueba_a" && r.ChosenOption == 0);
            Assert.True(world.Player.Stats.Smarts > smarts);
            Assert.True(world.Player.OwnRules[0].TimesUsed >= 2);
        }

        [Fact]
        public void Regla_EnUnDiaClave_NoSePuedeHacer()
        {
            EventDefinition key = Content().GetEvent("evento.clave");
            EventDefinition test = Content().GetEvent("evento.prueba_a");

            Assert.False(key.AllowsOwnRule);
            Assert.True(test.AllowsOwnRule);
        }

        [Fact]
        public void Regla_OpcionDeReglaSinGrupo_SeRechaza()
        {
            var ex = Assert.Throws<ContentLoadException>(() => Content(@"[ { ""id"": ""evento.x"", ""opciones"": [ { ""texto"": ""opcion.x"", ""regla"": ""estudiar"" } ] } ]"));

            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.x") && e.Message.Contains("grupo de reglas"));
        }

        /// <summary>Una vida en Chile que llega a un evento que admite regla.</summary>
        private static LifeSession UntilRuleEvent(ulong seed)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            var decider = new Decisions.AutoDecider(session.Content);
            var random = new Randomness.GameRandom(seed);
            while (session.PlayerAgeInYears < 18)
            {
                PendingEvent? pending = session.Pending;
                if (pending != null && session.Content.GetEvent(pending.EventId).AllowsOwnRule)
                {
                    return session;
                }

                if (pending != null)
                {
                    session.Choose(decider.Choose(session.World, pending, random));
                }
                else
                {
                    session.Advance(GameDate.WeeksPerYear);
                }
            }

            throw new Xunit.Sdk.XunitException("Ningún evento con regla antes de los 18.");
        }

        [Fact]
        public void Regla_DesdeLaPantalla_SeAnotaSeMuestraYSeBorra()
        {
            LifeSession session = UntilRuleEvent(4);
            var presenter = new LifePresenter(session);
            Assert.True(presenter.Snapshot().PendingEvent!.AllowsRule);

            presenter.ChooseAlways(0);

            RuleView rule = Assert.Single(presenter.Rules());
            Assert.False(string.IsNullOrEmpty(rule.Situation));
            Assert.False(string.IsNullOrEmpty(rule.Choice));
            Assert.Null(session.Pending);

            presenter.RemoveRule(rule.Key);
            Assert.Empty(presenter.Rules());
        }

        /// <summary>Juega una vida en Chile hasta esa edad, decidiendo como el simulador.</summary>
        private static LifeSession LiveUntil(int age, ulong seed = 5)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            var decider = new Decisions.AutoDecider(session.Content);
            var random = new Randomness.GameRandom(seed);
            while (session.PlayerAgeInYears < age)
            {
                PendingEvent? pending = session.Pending;
                if (pending != null)
                {
                    session.Choose(decider.Choose(session.World, pending, random));
                }
                else
                {
                    session.Advance(GameDate.WeeksPerYear);
                }
            }

            return session;
        }

        [Fact]
        public void Deseos_EnElColegio_NacenDeTuVidaYSeMarcanConDestacador()
        {
            // Algunas vidas ya cumplieron todo lo de este año: se busca una que siga queriendo algo.
            for (ulong seed = 5; seed < 25; seed++)
            {
                LifeSession session = LiveUntil(7, seed);
                var presenter = new LifePresenter(session);
                IReadOnlyList<WishView> wishes = presenter.Wishes();
                Assert.InRange(wishes.Count, 1, WishSystem.MaxPerStage);
                Assert.All(wishes, w => Assert.DoesNotContain(LifePresenter.PersonMark, w.Text));
                WishView? open = wishes.FirstOrDefault(w => w.Open);
                if (open == null)
                {
                    continue;
                }

                presenter.HighlightWish(open.Index, true);
                Assert.True(presenter.Wishes().Single(w => w.Index == open.Index).Highlighted);
                return;
            }

            throw new Xunit.Sdk.XunitException("Ninguna vida con un deseo abierto a los 7.");
        }

        [Fact]
        public void Deseo_SeCumpleCuandoPasaLoQueQuieresYQuedaEnElDiario()
        {
            var session = new LifeSession(RepoPacks.Content, 5, "pais.chile");
            World world = session.World;
            Character player = world.Player;
            player.Routine = new Routine("prueba");
            player.Wishes.Add(new Wish("deseo.celular_propio", "prueba", world.Date, null) { Highlighted = true });
            int happiness = player.Stats.Happiness;
            var cycle = new WeekCycle().Register(new WishSystem(session.Content));

            player.Phone = new Devices.Phone(world.Date);
            cycle.AdvanceWeeks(world, 5);

            Wish wish = player.Wishes[0];
            Assert.Equal(WishState.Done, wish.State);
            Assert.Contains(world.Journal, j => j.TextKey == "deseo.celular_propio.cumplido");
            Assert.Equal(happiness + 2 * WishSystem.FulfilledHappiness, player.Stats.Happiness);
        }

        [Fact]
        public void Deseo_QuePerseguiasYNoPaso_AlCambiarDeEtapaQuedaPerdido()
        {
            var session = new LifeSession(RepoPacks.Content, 5, "pais.chile");
            World world = session.World;
            Character player = world.Player;
            player.Routine = new Routine("etapa.nueva");
            player.Wishes.Add(new Wish("deseo.celular_propio", "etapa.vieja", world.Date, null) { Highlighted = true });
            player.Wishes.Add(new Wish("deseo.entrar_equipo", "etapa.vieja", world.Date, null));

            new WeekCycle().Register(new WishSystem(session.Content)).AdvanceWeeks(world, 1);

            Assert.Equal(WishState.Lost, player.Wishes[0].State);
            Assert.Equal(WishState.Faded, player.Wishes[1].State);
            Assert.Contains(world.Journal, j => j.TextKey == "deseo.celular_propio.perdido");
            Assert.DoesNotContain(world.Journal, j => j.TextKey == "deseo.entrar_equipo.perdido");
        }

        [Fact]
        public void ReglasYDeseos_SeGuardanYSeCargan()
        {
            LifeSession session = LiveUntil(7);
            Character player = session.World.Player;
            player.OwnRules.Add(new OwnRule("regla.prueba", "estudiar") { TimesUsed = 2 });
            player.Wishes.Add(new Wish("deseo.celular_propio", "etapa.x", session.World.Date, null) { Highlighted = true });

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            OwnRule rule = Assert.Single(loaded.World.Player.OwnRules);
            Assert.Equal(("regla.prueba", "estudiar", 2), (rule.Key, rule.Choice, rule.TimesUsed));
            Wish wish = loaded.World.Player.Wishes.Last();
            Assert.Equal(("deseo.celular_propio", "etapa.x", true), (wish.WishId, wish.StageKey, wish.Highlighted));
            Assert.Equal(player.Wishes.Count, loaded.World.Player.Wishes.Count);
        }
    }
}

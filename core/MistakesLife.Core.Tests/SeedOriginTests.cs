using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// R4: los errores que vuelven muestran de dónde vienen ("Esto viene de acá") y
    /// ofrecen volver al pasado ahí mismo, frente al error.
    /// </summary>
    public class SeedOriginTests
    {
        private const string SeedsJson = @"[
          { ""id"": ""semilla.copio"", ""tipo"": ""negativa"", ""fuerza"": 40, ""vence_anios"": 20 }
        ]";

        private const string EventsJson = @"[
          { ""id"": ""evento.prueba_copia"", ""lugar"": ""lugar.colegio"", ""peso"": 1000, ""unico"": true,
            ""opciones"": [
              { ""texto"": ""opcion.copiar"", ""efectos"": [""plantar('semilla.copio')""] },
              { ""texto"": ""opcion.no_copiar"" }
            ] },
          { ""id"": ""evento.copia_descubierta"", ""semilla"": ""semilla.copio"" }
        ]";

        [Fact]
        public void Semilla_PlantadaAlElegir_RecuerdaElMomentoLaOpcionYElLugar()
        {
            var content = new ContentDatabase();
            content.AddSeedsJson("pack.prueba", "seeds/prueba.json", SeedsJson);
            content.AddEventsJson("pack.prueba", "events/prueba.json", EventsJson);
            content.Build();
            World world = WorldFactory.CreateNewLife(7);
            var events = new EventSystem(content, new EventRules { WeeklyRandomEventChance = 1.0 });
            new WeekCycle().Register(events).AdvanceWeeks(world, 10);

            events.Choose(world, world.PendingEvents.Single(), 0);

            SeedOrigin origin = Assert.Single(world.Seeds.OpenOfType("semilla.copio")).Origin!;
            Assert.Equal("evento.prueba_copia", origin.EventId);
            Assert.Equal("opcion.copiar", origin.OptionTextKey);
            Assert.Equal("lugar.colegio", origin.PlaceKey);
        }

        [Fact]
        public void VidaDePrueba_AlMenosTresConsecuencias_MuestranDeDondeVienen()
        {
            var origins = new List<SeedOriginView>();
            Play(1, 60, (session, view) =>
            {
                if (view.Origin != null)
                {
                    origins.Add(view.Origin);
                }

                return false;
            });

            Assert.True(origins.Count >= 3, $"Solo {origins.Count} consecuencias mostraron de dónde vienen.");
            Assert.All(origins, o =>
            {
                Assert.Contains(" · ", o.When); // "14 de mayo · 9 años"
                Assert.False(string.IsNullOrWhiteSpace(o.Moment));
                Assert.DoesNotContain("evento.", o.Moment); // un texto de verdad, no una clave
                Assert.Equal("Esto viene de acá", o.Title);
            });
            Assert.Contains(origins, o => o.Choice != null && o.Choice.StartsWith("Elegiste: «"));
        }

        [Fact]
        public void Origen_SeGuardaYSeLeeConLaPartida()
        {
            LifeSession? found = null;
            Play(1, 60, (session, view) =>
            {
                found = view.Origin != null ? session : null;
                return found != null;
            });
            LifeSession session = Assert.IsType<LifeSession>(found);
            SeedOriginView before = new LifePresenter(session).Snapshot().PendingEvent!.Origin!;

            LifeSession loaded = LifeSession.LoadFromBytes(RepoPacks.Content, session.SaveToBytes());
            SeedOriginView after = new LifePresenter(loaded).Snapshot().PendingEvent!.Origin!;

            Assert.Equal(before.When, after.When);
            Assert.Equal(before.Moment, after.Moment);
            Assert.Equal(before.Choice, after.Choice);
            Assert.Equal(before.Person, after.Person);
            Assert.Equal(before.Place, after.Place);
        }

        [Fact]
        public void VolverAlOrigen_VuelveAAntesDeEseDiaYGastaUnaCarga()
        {
            LifeSession? found = null;
            Play(1, 60, (session, view) =>
            {
                found = view.Origin?.SnapshotId != null ? session : null;
                return found != null;
            });
            LifeSession session = Assert.IsType<LifeSession>(found);
            Seed seed = session.World.Seeds.Get(session.Pending!.SeedId!.Value)!;
            GameDate planted = seed.PlantedDate;
            int charges = session.TimeMachine.Charges;

            new LifePresenter(session).GoBackToOrigin();

            Assert.True(session.World.Date <= planted, $"Volvió al {session.World.Date}, después del día del error ({planted}).");
            Assert.Equal(charges - 1, session.TimeMachine.Charges);
        }

        /// <summary>
        /// Juega una vida con las decisiones de una persona cualquiera hasta la edad dada.
        /// En cada momento que vuelve de una semilla llama a <paramref name="stop"/>; si responde true, se detiene ahí.
        /// </summary>
        private static void Play(ulong seed, int years, System.Func<LifeSession, EventView, bool> stop)
        {
            var session = new LifeSession(RepoPacks.Content, seed);
            var presenter = new LifePresenter(session);
            var decider = new AutoDecider(RepoPacks.Content);
            GameRandom random = new GameRandom(seed).Fork("decisiones");
            int target = years * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                PendingEvent? pending = session.Pending;
                if (pending == null)
                {
                    session.Advance(4);
                    continue;
                }

                if (pending.SeedId.HasValue && stop(session, presenter.Snapshot().PendingEvent!))
                {
                    return;
                }

                session.Choose(decider.Choose(session.World, pending, random));
            }

        }
    }
}

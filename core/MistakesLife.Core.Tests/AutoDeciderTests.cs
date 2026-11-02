using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class AutoDeciderTests
    {
        private const string EventsJson = @"[
          { ""id"": ""evento.juguete"", ""opciones"": [
            { ""texto"": ""opcion.compartir"", ""tendencia"": ""jugador.amabilidad"" },
            { ""texto"": ""opcion.no_compartir"", ""tendencia"": ""100 - jugador.amabilidad"" },
            { ""texto"": ""opcion.sin_tendencia"" } ] }
        ]";

        private static ContentDatabase Content()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", EventsJson);
            return content.Build();
        }

        /// <summary>Una vida nueva cuyo jugador es muy amable (o muy poco amable).</summary>
        private static (World World, PendingEvent Pending) Pending(bool kind)
        {
            World world = Enumerable.Range(1, 500)
                .Select(seed => WorldFactory.CreateNewLife((ulong)seed))
                .First(w => kind ? w.Player.Personality.Agreeableness >= 75 : w.Player.Personality.Agreeableness <= 25);
            var pending = new PendingEvent("evento.juguete", world.Player.Id, world.Date, null, new[] { 0, 1, 2 });
            return (world, pending);
        }

        [Fact]
        public void Pesos_SalenDeLaTendenciaYSinTendenciaValen50()
        {
            var (world, pending) = Pending(kind: true);
            int a = world.Player.Personality.Agreeableness;

            var weights = new AutoDecider(Content()).Weights(world, pending);

            Assert.Equal(new[] { (double)a, 100.0 - a, AutoDecider.DefaultTendency }, weights.ToArray());
        }

        [Fact]
        public void PersonaAmable_CompartePorLejosMasQueUnaPocoAmable()
        {
            var decider = new AutoDecider(Content());
            int Shares(bool kind)
            {
                var (world, pending) = Pending(kind);
                var random = new GameRandom(3);
                return Enumerable.Range(0, 1000).Count(_ => decider.Choose(world, pending, random) == 0);
            }

            Assert.True(Shares(kind: true) > Shares(kind: false) * 2);
        }

        [Fact]
        public void FuncionSi_EligeSegunLaCondicion()
        {
            var world = WorldFactory.CreateNewLife(1);
            var scope = new EventScope(world, Content(), world.Player, null, readOnly: true);

            Assert.Equal(Value.FromNumber(7), Expression.Parse("si(jugador.edad < 1, 7, 2)").Evaluate(scope));
            Assert.Equal(Value.FromNumber(2), Expression.Parse("si(jugador.tiene('x'), 7, 2)").Evaluate(scope));
        }

        [Fact]
        public void TendenciaMalEscrita_SeRechazaAlCargar()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[
              { ""id"": ""evento.x"", ""opciones"": [ { ""texto"": ""opcion.a"", ""tendencia"": ""jugador.no_existe +"" } ] } ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.x"));
        }
    }
}

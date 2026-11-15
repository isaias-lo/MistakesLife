using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R6d2: ser socios (un negocio chico con una persona de tu vida).</summary>
    public class VentureTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static VenturesDefinition Rules => Content.GetCountry("pais.chile").Economy!.Ventures!;

        /// <summary>Una vida a los 30 con plata para poner y una amistad nueva (devuelve su id).</summary>
        private static (LifeSession Session, int FriendId) WithFriend(ulong seed = 5)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (30 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            world.Player.Money = 5_000_000;
            var before = RelationshipRules.WithBond(world, world.Player, Characters.RelationshipBond.Friend).Select(c => c.Id).ToList();
            Scripting.Effect.Parse("jugador.conocer('amistad')").Apply(new EventScope(world, Content, world.Player, null, readOnly: false));
            int friend = RelationshipRules.WithBond(world, world.Player, Characters.RelationshipBond.Friend).Select(c => c.Id).Except(before).Single();
            return (session, friend);
        }

        private static void Choose(LifeSession session, string optionKey)
        {
            PendingEvent pending = session.Pending!;
            EventDefinition ev = Content.Events.Single(e => e.Id == pending.EventId);
            session.Choose(pending.AvailableOptions.Single(i => ev.Options[i].TextKey == optionKey));
        }

        [Fact]
        public void Cierre_ElPrimerAnoComoLasMicroempresasDeChile()
        {
            double monthly = VentureRules.MonthlyClosure(Rules, 0, VentureRules.Quality(50, 50));
            Assert.Equal(1 - Rules.FirstYearClosure, System.Math.Pow(1 - monthly, 12), 6);

            // Llevarlo bien lo hace cerrar menos; mal, más.
            Assert.True(VentureRules.MonthlyClosure(Rules, 0, VentureRules.Quality(90, 90)) < monthly);
            Assert.True(VentureRules.MonthlyClosure(Rules, 0, VentureRules.Quality(10, 10)) > monthly);
            Assert.True(VentureRules.MonthlyClosure(Rules, 24, 1) < monthly);
        }

        [Fact]
        public void Suerte_PromedioUnoYLaMayoriaBajoElPromedio()
        {
            var random = new GameRandom(7);
            double[] draws = Enumerable.Range(0, 40000).Select(_ => VentureRules.LogNormal(random, Rules.Spread)).ToArray();
            Assert.InRange(draws.Average(), 0.85, 1.15);

            // En la Encuesta de Microemprendimiento 2025, el 76,9 % gana el promedio o menos.
            double below = draws.Count(d => d <= 1) / (double)draws.Length;
            Assert.InRange(below, 0.74, 0.80);
        }

        [Fact]
        public void Socios_ConUnaAmistad_PonenPlataYCadaMesRecibeSuParte()
        {
            (LifeSession session, int friend) = WithFriend();
            World world = session.World;
            Assert.Contains(session.RequestableWith(friend), e => e.Id == "evento.futuro.socios_amigo");

            decimal before = world.Player.Money;
            Assert.True(session.Request("evento.futuro.socios_amigo", friend));
            Choose(session, "opcion.futuro.socios.formal");
            Assert.NotNull(world.Player.Venture);
            Assert.True(world.Player.Venture!.Formal);
            Assert.True(world.Player.Money < before);
            Assert.Equal(friend, VentureRules.Partner(world, world.Player)?.Id);
            Assert.Contains(world.Journal, j => j.TextKey == VentureRules.OpenedJournal);
            Assert.DoesNotContain(session.RequestableWith(friend), e => e.Id == "evento.futuro.socios_amigo");
            Assert.Contains(session.RequestableWith(friend), e => e.Id == "evento.negocio.cerrar");

            for (int i = 0; i < 10 && world.Player.Venture != null; i++)
            {
                while (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions.Last());
                }

                session.Advance(1);
            }

            Assert.True(world.Player.Venture == null || world.Player.Venture.MonthsOpen >= 2);
            if (world.Player.Venture != null)
            {
                Assert.True(world.Player.Venture.TotalShare > 0);
                LifeSession loaded = LifeSession.Load(Content, session.SaveToText());
                Assert.Equal(world.Player.Venture.TotalShare, loaded.World.Player.Venture!.TotalShare);
                Assert.Equal(world.Player.Venture.Luck, loaded.World.Player.Venture.Luck);
            }
        }

        [Fact]
        public void Socios_CerrarDeComunAcuerdo_TerminaElNegocio()
        {
            (LifeSession session, int friend) = WithFriend();
            World world = session.World;
            Assert.True(session.Request("evento.futuro.socios_amigo", friend));
            Choose(session, "opcion.futuro.socios.informal");
            Assert.False(world.Player.Venture!.Formal);

            Assert.True(session.Request("evento.negocio.cerrar", friend));
            Choose(session, "opcion.negocio.cerrar.acuerdo");
            Assert.Null(world.Player.Venture);
            Assert.Contains(world.Journal, j => j.TextKey == VentureRules.ClosedJournal);
        }

        [Fact]
        public void Socios_SinPlataParaElAporte_NoAparece()
        {
            (LifeSession session, int friend) = WithFriend();
            session.World.Player.Money = 0;
            Assert.DoesNotContain(session.RequestableWith(friend), e => e.Id == "evento.futuro.socios_amigo");
        }
    }
}

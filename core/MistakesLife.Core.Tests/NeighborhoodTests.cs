using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5c2: Tu gente abre el mapa de tu barrio; tocas un lugar, eliges qué hacer y con quién.</summary>
    public class NeighborhoodTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida en ese país jugada hasta esa edad (eligiendo la primera opción de cada momento).</summary>
        private static LifeSession SessionAt(int age, string country = "pais.chile", ulong seed = 3)
        {
            var session = new LifeSession(Content, seed, country);
            session.TimeMachine.Enabled = false;
            int target = age * GameDate.WeeksPerYear + 10;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }

            Settle(session);
            return session;
        }

        private static void Settle(LifeSession session)
        {
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }
        }

        /// <summary>Avanza semana a semana hasta que el mes sea ese.</summary>
        private static void UntilMonth(LifeSession session, int month)
        {
            while (session.World.Date.Month != month)
            {
                session.Advance(1);
                Settle(session);
            }
        }

        [Fact]
        public void Barrio_TieneLosLugaresDeBaseYTuCasa()
        {
            LifeSession session = SessionAt(15);

            NeighborhoodView map = new LifePresenter(session).Neighborhood()!;

            string[] ids = map.Places.Select(p => p.Id).ToArray();
            Assert.Contains("lugar.barrio.casa", ids);
            Assert.Contains("lugar.barrio.plaza", ids);
            Assert.Contains("lugar.barrio.mall", ids);
            Assert.Contains("lugar.barrio.parque", ids);
            Assert.Contains("lugar.barrio.gimnasio", ids);
            Assert.Contains("lugar.barrio.cafe", ids);
            Assert.DoesNotContain("lugar.barrio.bar", ids);
            Assert.Equal("casa", map.Places.First(p => p.Id == "lugar.barrio.casa").Opens);
            Assert.StartsWith("Tu gente · ", map.Title);
            Assert.InRange(map.Closest.Count, 1, LifePresenter.MaxClosest);
        }

        [Fact]
        public void Barrio_ElBarSoloDesdeLos18()
        {
            LifeSession session = SessionAt(19);

            NeighborhoodView map = new LifePresenter(session).Neighborhood()!;

            Assert.Contains(map.Places, p => p.Id == "lugar.barrio.bar");
        }

        [Fact]
        public void Barrio_LaFondaEsDeChileYSoloEnSeptiembre()
        {
            LifeSession chile = SessionAt(15);
            UntilMonth(chile, 8);
            Assert.DoesNotContain(new LifePresenter(chile).Neighborhood()!.Places, p => p.Id == "lugar.chile.fonda");

            UntilMonth(chile, 9);
            MapPlaceView fonda = new LifePresenter(chile).Neighborhood()!.Places.Single(p => p.Id == "lugar.chile.fonda");
            Assert.Equal("Solo en Chile", fonda.CountryNote);

            LifeSession argentina = SessionAt(15, "pais.argentina");
            UntilMonth(argentina, 9);
            NeighborhoodView map = new LifePresenter(argentina).Neighborhood()!;
            Assert.DoesNotContain(map.Places, p => p.Id == "lugar.chile.fonda");
            Assert.Contains(map.Places, p => p.Id == "lugar.argentina.cancha_futbol5" && p.CountryNote == "Solo en Argentina");
        }

        [Fact]
        public void Lugar_ElCineDiceLoQueCuestaYLoQueTrae()
        {
            var presenter = new LifePresenter(SessionAt(15));
            PlaceActivityView cine = presenter.PlaceActivities("lugar.barrio.mall").Single(a => a.Id == "accion.barrio.mall.cine");

            Assert.Equal("Ver una película", cine.Title);
            Assert.Contains("1 tarde", cine.Costs);
            Assert.Contains("Lo paga tu casa", cine.Costs);
            Assert.Contains("+ relación", cine.Gives);
            Assert.True(cine.CanGoAlone);
            Assert.True(cine.WithSomeone);
            Assert.True(cine.Companions > 0);

            LifeSession adult = SessionAt(19);
            adult.World.Player.Money = 1_000_000;
            PlaceActivityView paid = new LifePresenter(adult).PlaceActivities("lugar.barrio.mall").Single(a => a.Id == "accion.barrio.mall.cine");
            decimal price = Life.ActionRules.Price(adult.World, adult.World.Player, Content.Action("accion.barrio.mall.cine")!);
            Assert.True(price > 0);
            Assert.Contains(paid.Costs, c => c.StartsWith("$"));
        }

        [Fact]
        public void Lugar_SinPlata_NoSePuedeYDicePorQue()
        {
            LifeSession adult = SessionAt(19);
            adult.World.Player.Money = 0;

            PlaceActivityView cine = new LifePresenter(adult).PlaceActivities("lugar.barrio.mall").Single(a => a.Id == "accion.barrio.mall.cine");

            Assert.False(cine.CanAfford);
            Assert.Equal("No te alcanza la plata", cine.Blocked);
            Assert.Throws<System.InvalidOperationException>(() => new LifePresenter(adult).DoAction("accion.barrio.mall.cine", null));
        }

        [Fact]
        public void Cine_SoloOConAlguien_CobraTuParteYSeRecuerda()
        {
            LifeSession session = SessionAt(19);
            Character player = session.World.Player;
            player.Money = 1_000_000;
            var presenter = new LifePresenter(session);
            decimal price = Life.ActionRules.Price(session.World, player, Content.Action("accion.barrio.mall.cine")!);

            ActionResultView alone = presenter.DoAction("accion.barrio.mall.cine", null);
            Assert.StartsWith("Fuiste al cine", alone.Text);
            Assert.Equal(1_000_000 - price, player.Money);

            CompanionView friend = presenter.Companions("accion.barrio.mall.comer").Suggested.First();
            ActionResultView together = presenter.DoAction("accion.barrio.mall.comer", friend.Id);
            Assert.Contains(session.World.GetCharacter(friend.Id).FirstName, together.Text);
            Assert.Equal(session.World.Date.TotalWeeks, player.RelationshipWith(friend.Id)!.LastTogetherWeek);
        }

        [Fact]
        public void ConQuien_TresSugeridosConMotivoYTodosEnSusPestanas()
        {
            var presenter = new LifePresenter(SessionAt(15));

            CompanionsView with = presenter.Companions("accion.barrio.mall.cine");

            Assert.InRange(with.Suggested.Count, 1, LifePresenter.MaxSuggested);
            Assert.Equal(with.Total, with.Tabs.Sum(t => t.People.Count));
            Assert.All(with.Tabs, t => Assert.Equal(t.People.OrderByDescending(p => p.Closeness).Select(p => p.Id), t.People.Select(p => p.Id)));
            Assert.All(with.Tabs, t => Assert.Contains(t.Key, LifePresenter.CompanionTabs));
            Assert.Contains(with.Tabs, t => t.Key == "familia");
            Assert.NotNull(with.Alone);
            Assert.Contains("en tu gente", with.ChooseOther);
        }

        [Fact]
        public void ConQuien_DespuesDeSalir_ElMotivoEsQueSalieronHacePoco()
        {
            LifeSession session = SessionAt(15);
            var presenter = new LifePresenter(session);
            CompanionView first = presenter.Companions("accion.barrio.plaza.vuelta").Suggested.First();
            presenter.DoAction("accion.barrio.plaza.vuelta", first.Id);

            CompanionView again = presenter.Companions("accion.barrio.mall.comer").Tabs.SelectMany(t => t.People).Single(p => p.Id == first.Id);

            Assert.True(again.Reason == "Salieron hace poco" || again.Reason == Content.Text("accion.barrio.mall.comer.le_gusta"));
        }

        [Fact]
        public void AccionesDelBarrio_NoVanEnLaListaNiEnLaFicha()
        {
            var presenter = new LifePresenter(SessionAt(15));
            CompanionView someone = presenter.Companions("accion.barrio.mall.cine").Suggested.First();

            Assert.DoesNotContain(presenter.Actions("gente"), a => a.Id.StartsWith("accion.barrio."));
            Assert.DoesNotContain(presenter.PersonCard(someone.Id)!.Actions, a => a.Id.StartsWith("accion.barrio."));
        }

        [Fact]
        public void Guardado_RecuerdaLaUltimaVezQueSalieronJuntos()
        {
            LifeSession session = SessionAt(15);
            var presenter = new LifePresenter(session);
            CompanionView first = presenter.Companions("accion.barrio.plaza.vuelta").Suggested.First();
            presenter.DoAction("accion.barrio.plaza.vuelta", first.Id);

            LifeSession loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes());

            Assert.Equal(session.World.Date.TotalWeeks, loaded.World.Player.RelationshipWith(first.Id)!.LastTogetherWeek);
        }
    
        [Fact]
        public void Salida_SeVeEnSuLugarConQuienFuiste()
        {
            var presenter = new LifePresenter(SessionAt(15));
            CompanionView someone = presenter.Companions("accion.barrio.mall.cine").Suggested.First();

            OutingView with = presenter.Outing("accion.barrio.mall.cine", someone.Id)!;
            OutingView alone = presenter.Outing("accion.barrio.mall.cine", null)!;

            Assert.Equal("mall", with.Shape);
            Assert.Equal(someone.Id, with.Companion!.Id);
            Assert.StartsWith("Mall y cine · con ", with.Title);
            Assert.True(with.Player.IsPlayer);
            Assert.Null(alone.Companion);
            Assert.Equal("Mall y cine", alone.Title);
            Assert.Equal("casa_amigos", presenter.Outing("accion.juntarse_amigo", null)!.Shape);
            Assert.Null(presenter.Outing("accion.conversar", null));
        }
}
}

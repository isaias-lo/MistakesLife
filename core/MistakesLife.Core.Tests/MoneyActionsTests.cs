using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// R7b: las decisiones de plata ya no llegan solas cada año; las pides tú desde Tu dinero.
    /// Lo que sí llega solo (la cobranza, el fin de mes en rojo, la tarjeta) tiene tope y variantes.
    /// </summary>
    public class MoneyActionsTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static readonly string[] WalletEvents =
        {
            "evento.revisar_compra", "evento.revisar_plan_celular", "evento.revisar_transporte", "evento.revisar_internet",
            "evento.revisar_cuentas", "evento.revisar_aporte_a_la_casa", "evento.chile.revisar_gastos_personales",
            "evento.irse_de_la_casa", "evento.dinero.repactar",
        };

        [Fact]
        public void RevisarTusGastos_SeVuelveUnBotonDeTuDinero_YNoPasaSolo()
        {
            foreach (string id in WalletEvents)
            {
                EventDefinition ev = Content.GetEvent(id);
                Assert.True(ev.OnRequest != null, id + " debe ser a_pedido");
                Assert.Equal(LifePresenter.WalletPlace, ev.Place);
                Assert.Null(ev.AnnualProbability);
            }
        }

        [Fact]
        public void LoQueLlegaSolo_TieneTopeYUnTextoDistintoCadaVez()
        {
            foreach (string id in new[] { "evento.cobranza", "evento.fin_de_mes_en_rojo", "evento.compra_tarjeta", "evento.oferta_tarjeta" })
            {
                EventDefinition ev = Content.GetEvent(id);
                Assert.True(ev.MaxTimes.HasValue, id + " necesita veces_maximas");
                Assert.Equal(ev.MaxTimes!.Value, ev.Variants.Count + 1);
            }
        }

        [Fact]
        public void TuDinero_ConPegaYViviendoConTusPapas_OfreceIrteDeLaCasa_YAlApretarloLlegaLaCarta()
        {
            var session = new LifeSession(Content, 5, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (24 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            foreach (string effect in new[] { "jugador.dejar_trabajo()", "jugador.desmarcar('vive_independiente')", "jugador.marcar('trabajando')", "jugador.trabajar_en('oficio.vendedor.area.vestuario')" })
            {
                Scripting.Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));
            }

            var presenter = new LifePresenter(session);
            ActionView leave = Assert.Single(presenter.MoneyActions(), a => a.EventId == "evento.irse_de_la_casa");
            Assert.Equal("Irte de la casa de tus papás", leave.Label);
            Assert.DoesNotContain(presenter.Team()?.Actions ?? new ActionView[0], a => a.EventId == "evento.irse_de_la_casa");

            Assert.True(session.Request(leave.EventId));

            Assert.Equal("evento.irse_de_la_casa", session.Pending!.EventId);
        }
    }
}

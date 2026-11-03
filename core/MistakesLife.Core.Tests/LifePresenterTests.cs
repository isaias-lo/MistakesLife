using System;
using System.Linq;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class LifePresenterTests
    {
        private static LifePresenter NewGame(ulong seed) => new LifePresenter(new LifeSession(RepoPacks.Content, seed));

        /// <summary>Avanza hasta que haya un evento esperando decisión.</summary>
        private static LifePresenter UntilEvent(ulong seed)
        {
            LifePresenter presenter = NewGame(seed);
            while (presenter.Phase == LifePhase.Living)
            {
                presenter.AdvanceUntilSomething();
            }

            return presenter;
        }

        [Fact]
        public void Vista_AlNacer_MuestraNombreLugarFamiliaYEdadEnSemanas()
        {
            LifeSnapshot snapshot = NewGame(7).Snapshot();

            Assert.Equal(LifePhase.Living, snapshot.Phase);
            Assert.False(string.IsNullOrWhiteSpace(snapshot.PlayerName));
            Assert.Equal("Puente Alto, Santiago, Chile", snapshot.Place);
            Assert.Equal("0 semanas", snapshot.AgeLabel);
            Assert.Matches(@"^Año \d+, semana \d+$", snapshot.DateLabel);
            Assert.Contains(snapshot.Family, p => p.Relation == "Mamá");
            Assert.Null(snapshot.PendingEvent);
            Assert.Equal(7UL, snapshot.Seed);
            Assert.Equal(1, snapshot.Generation);
        }

        [Fact]
        public void Vista_ConEventoPendiente_MuestraTextoYOpcionesTraducidas()
        {
            LifeSnapshot snapshot = UntilEvent(7).Snapshot();

            Assert.Equal(LifePhase.Choosing, snapshot.Phase);
            EventView view = Assert.IsType<EventView>(snapshot.PendingEvent);
            Assert.NotEqual(view.EventId, view.Text);
            Assert.NotEmpty(view.Options);
            Assert.All(view.Options, o => Assert.False(o.StartsWith("opcion.", StringComparison.Ordinal)));
        }

        [Fact]
        public void Elegir_UnaOpcion_ResuelveElEvento()
        {
            LifePresenter presenter = UntilEvent(7);

            var pending = presenter.Session.Pending;

            presenter.Choose(0);

            Assert.NotSame(pending, presenter.Session.Pending);
        }

        [Fact]
        public void Elegir_PosicionFueraDeRango_DaError()
        {
            LifePresenter presenter = UntilEvent(7);
            int options = presenter.Snapshot().PendingEvent!.Options.Count;

            Assert.Throws<ArgumentOutOfRangeException>(() => presenter.Choose(options));
            Assert.Throws<ArgumentOutOfRangeException>(() => presenter.Choose(-1));
        }

        [Fact]
        public void Avanzar_ConEventoPendiente_NoMueveElTiempo()
        {
            LifePresenter presenter = UntilEvent(7);
            string date = presenter.Snapshot().DateLabel;

            Assert.Equal(0, presenter.AdvanceWeek());
            Assert.Equal(0, presenter.AdvanceUntilSomething());
            Assert.Equal(date, presenter.Snapshot().DateLabel);
        }

        [Fact]
        public void Avanzar_NuncaPasaMasDeCincoAnosSeguidos()
        {
            LifePresenter presenter = NewGame(7);

            int weeks = presenter.AdvanceUntilSomething();

            Assert.InRange(weeks, 1, LifePresenter.MaxYearsPerStep * 52);
        }

        [Fact]
        public void Diario_LineasNuevas_SeEntreganUnaSolaVez()
        {
            LifePresenter presenter = NewGame(7);
            presenter.AdvanceUntilSomething();

            var first = presenter.TakeNewJournal();
            var second = presenter.TakeNewJournal();

            Assert.Contains(first, l => l.Text == "Nací.");
            Assert.Empty(second);
        }

        [Fact]
        public void Vida_HastaLaMuerte_TerminaOEligeHeredero()
        {
            LifePresenter presenter = NewGame(7);
            for (int step = 0; step < 5000 && (presenter.Phase == LifePhase.Living || presenter.Phase == LifePhase.Choosing); step++)
            {
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else
                {
                    presenter.AdvanceUntilSomething();
                }
            }

            LifeSnapshot end = presenter.Snapshot();
            Assert.True(end.Phase == LifePhase.Over || end.Phase == LifePhase.ChoosingHeir);
            if (end.Phase == LifePhase.ChoosingHeir)
            {
                Assert.NotEmpty(end.Heirs);
                presenter.ContinueWith(0);
                Assert.Equal(2, presenter.Snapshot().Generation);
                Assert.Equal(end.Heirs[0].Name, presenter.Snapshot().PlayerName);
            }
            else
            {
                Assert.Empty(end.Heirs);
            }
        }

        [Fact]
        public void Dinero_UsaElSeparadorDeMilesDelIdioma()
        {
            LifePresenter presenter = NewGame(7);
            presenter.Session.World.Player.Money = 1250000.4m;

            Assert.Equal("$1.250.000", presenter.Snapshot().MoneyLabel);
        }

        [Fact]
        public void Textos_DeLaPantalla_ExistenEnElIdiomaBase()
        {
            foreach (string key in LifePresenter.TextKeys)
            {
                Assert.NotEqual(key, RepoPacks.Content.Text(key));
            }
        }
    }
}

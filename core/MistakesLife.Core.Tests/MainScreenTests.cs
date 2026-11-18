using System.Linq;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>La pantalla principal (D1 en docs/roadmap.md): cabecera, barras de la vida y los botones Un mes y Un año.</summary>
    public class MainScreenTests
    {
        private static LifePresenter NewGame(ulong seed) => new LifePresenter(new LifeSession(RepoPacks.Content, seed));

        /// <summary>Responde lo que esté pendiente con la primera opción, para seguir viviendo.</summary>
        private static void AnswerPending(LifePresenter presenter)
        {
            while (presenter.Phase == LifePhase.Choosing)
            {
                presenter.Choose(0);
            }
        }

        [Fact]
        public void SemanasAlMesQueViene_LleganJustoALaPrimeraSemanaDelMes()
        {
            for (int week = 0; week < GameDate.WeeksPerYear * 2; week++)
            {
                var date = new GameDate(week);
                int weeks = date.WeeksToNextMonth;
                Assert.InRange(weeks, 1, 5);
                Assert.True(date.AddWeeks(weeks).IsFirstWeekOfMonth);
                for (int before = 1; before < weeks; before++)
                {
                    Assert.False(date.AddWeeks(before).IsFirstWeekOfMonth);
                }
            }
        }

        [Fact]
        public void SeguirUnMes_SiNoPasaNada_TerminaAlEmpezarElMesQueViene()
        {
            LifePresenter presenter = NewGame(11);
            int calm = 0;
            for (int step = 0; step < 60 && presenter.Phase != LifePhase.Over; step++)
            {
                AnswerPending(presenter);
                if (presenter.Phase != LifePhase.Living)
                {
                    break;
                }

                GameDate before = presenter.Session.World.Date;
                int expected = before.WeeksToNextMonth;
                int weeks = presenter.AdvanceMonth();
                Assert.InRange(weeks, 0, expected);
                if (presenter.Phase == LifePhase.Living)
                {
                    // Nada lo detuvo: llegó justo al primer lunes del mes que viene.
                    Assert.Equal(expected, weeks);
                    Assert.True(presenter.Session.World.Date.IsFirstWeekOfMonth);
                    calm++;
                }
            }

            Assert.True(calm > 0, "En 60 meses no hubo ni uno sin momentos: la prueba no comprobó nada.");
        }

        [Fact]
        public void UnAno_NuncaPasaDeUnAno()
        {
            LifePresenter presenter = NewGame(12);
            for (int step = 0; step < 15; step++)
            {
                AnswerPending(presenter);
                if (presenter.Phase != LifePhase.Living)
                {
                    break;
                }

                Assert.InRange(presenter.AdvanceYear(), 0, GameDate.WeeksPerYear);
            }
        }

        [Fact]
        public void Cabecera_DiceElMesElDiaYLaComuna()
        {
            LifePresenter presenter = NewGame(5);
            presenter.AdvanceMonth();
            AnswerPending(presenter);
            HeaderView header = presenter.Header();
            GameDate date = presenter.Session.World.Date;

            Assert.Equal(presenter.Text("ui.mes." + date.Month), header.Month.ToLowerInvariant());
            Assert.True(char.IsUpper(header.Month[0]));
            Assert.Contains(date.DayOfMonth.ToString(), header.Day);
            Assert.Equal(presenter.Text(presenter.Session.World.Player.Residence!.CommuneId), header.Commune);
        }

        [Fact]
        public void Barras_SonLasQueTieneElPersonaje_ConSuValorReal()
        {
            LifePresenter presenter = NewGame(5);
            var stats = presenter.Session.World.Player.Stats;
            var bars = presenter.LifeBars();

            Assert.Equal(new[] { "felicidad", "salud", "inteligencia", "apariencia", "energia" }, bars.Select(b => b.Id).ToArray());
            Assert.Equal(new[] { stats.Happiness, stats.Health, stats.Smarts, stats.Looks, stats.Energy }, bars.Select(b => b.Value).ToArray());
            Assert.All(bars, b => Assert.Equal(LifeBarView.LevelOf(b.Value), b.Level));
            Assert.All(bars, b => Assert.False(b.Label.StartsWith("ui.", System.StringComparison.Ordinal)));
        }

        [Theory]
        [InlineData(0, LifeBarLevel.Low)]
        [InlineData(39, LifeBarLevel.Low)]
        [InlineData(40, LifeBarLevel.Middle)]
        [InlineData(59, LifeBarLevel.Middle)]
        [InlineData(60, LifeBarLevel.Good)]
        [InlineData(100, LifeBarLevel.Good)]
        public void Barra_VerdeAmarillaORoja_SegunSuValor(int value, LifeBarLevel level)
        {
            Assert.Equal(level, LifeBarView.LevelOf(value));
        }

        [Fact]
        public void LineasDelDiario_TraenElDiaEnQuePasaron()
        {
            LifePresenter presenter = NewGame(5);
            for (int i = 0; i < 6 && presenter.FullJournal().Count == 0; i++)
            {
                AnswerPending(presenter);
                presenter.AdvanceYear();
            }

            Assert.NotEmpty(presenter.FullJournal());
            Assert.All(presenter.FullJournal(), line => Assert.Contains(" de ", line.DateLabel));
        }
    }
}

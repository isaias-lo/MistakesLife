using System.Linq;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Lo que el núcleo le da a la pantalla para la primera impresión: nacer, el resumen de cada paso y los momentos clave.</summary>
    public class FirstImpressionTests
    {
        private static LifePresenter NewGame(ulong seed) => new LifePresenter(new LifeSession(RepoPacks.Content, seed));

        [Fact]
        public void Nacer_MuestraNombreSexoLugarHogarYPadres()
        {
            LifePresenter presenter = NewGame(7);

            BirthView birth = presenter.Birth();

            Assert.Equal(presenter.Snapshot().PlayerName, birth.Name);
            Assert.Equal(birth.IsFemale ? "Niña" : "Niño", birth.SexLabel);
            Assert.Equal("Puente Alto, Santiago, Chile", birth.Place);
            Assert.StartsWith("Hogar", birth.HomeLabel);
            Assert.Contains(birth.Parents, p => p.Relation == "Mamá" && p.AgeInYears >= 14);
        }

        [Fact]
        public void Nacer_PartidasDistintas_DanVidasDistintas()
        {
            var births = new ulong[] { 1, 2, 3, 4, 5 }.Select(seed => NewGame(seed).Birth().Name).ToList();

            Assert.True(births.Distinct().Count() > 1);
        }

        [Theory]
        [InlineData(0, "ui.hogar.justo")]
        [InlineData(34, "ui.hogar.justo")]
        [InlineData(35, "ui.hogar.medio")]
        [InlineData(69, "ui.hogar.medio")]
        [InlineData(70, "ui.hogar.acomodado")]
        [InlineData(100, "ui.hogar.acomodado")]
        public void Hogar_SegunSuNivel_TieneSuFrase(int level, string key)
        {
            Assert.Equal(key, LifePresenter.HomeLevelKey(level));
        }

        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "Pasó una semana")]
        [InlineData(3, "Pasaron 3 semanas")]
        [InlineData(52, "Pasó 1 año")]
        [InlineData(56, "Pasó 1 año y 4 semanas")]
        [InlineData(104, "Pasaron 2 años")]
        [InlineData(107, "Pasaron 2 años y 3 semanas")]
        public void TiempoQuePaso_SeDiceEnPalabras(int weeks, string expected)
        {
            Assert.Equal(expected, NewGame(7).ElapsedLabel(weeks));
        }

        [Fact]
        public void Resumen_DespuesDeAvanzar_CuentaElTiempoYLosCambios()
        {
            LifePresenter presenter = NewGame(7);
            LifeSnapshot before = presenter.Snapshot();

            int weeks = presenter.AdvanceUntilSomething();
            LifeSnapshot after = presenter.Snapshot();
            StepSummary summary = presenter.Summarize(before, after, weeks);

            Assert.Equal(weeks, summary.Weeks);
            Assert.StartsWith("Pas", summary.ElapsedLabel);
            int expected = (after.Health != before.Health ? 1 : 0)
                + (after.Happiness != before.Happiness ? 1 : 0)
                + (after.Smarts != before.Smarts ? 1 : 0)
                + (System.Math.Abs(System.Math.Round(after.Money - before.Money)) >= 1 ? 1 : 0);
            Assert.Equal(expected, summary.Changes.Count);
        }

        [Fact]
        public void Resumen_CadaCambio_LlevaSuSignoYSiEsBueno()
        {
            LifePresenter presenter = NewGame(7);
            LifeSnapshot before = presenter.Snapshot();
            LifeSnapshot after = WithStats(before, before.Health - 4, before.Happiness + 6, before.Smarts, before.Money - 45000m);

            StepSummary summary = presenter.Summarize(before, after, 0);

            Assert.Equal(new[] { "Salud -4", "Felicidad +6", "Dinero -$45.000" }, summary.Changes.Select(c => c.Label));
            Assert.Equal(new[] { false, true, false }, summary.Changes.Select(c => c.IsGood));
            Assert.Equal(string.Empty, summary.ElapsedLabel);
        }

        [Fact]
        public void Resumen_SinCambiosNiTiempo_EstaVacio()
        {
            LifePresenter presenter = NewGame(7);
            LifeSnapshot snapshot = presenter.Snapshot();

            Assert.True(presenter.Summarize(snapshot, snapshot, 0).IsEmpty);
        }

        [Fact]
        public void Resumen_EntreVidasDistintas_NoComparaNada()
        {
            LifeSnapshot first = NewGame(7).Snapshot();
            LifeSnapshot other = NewGame(8).Snapshot();

            Assert.True(NewGame(7).Summarize(first, other, 10).IsEmpty);
        }

        [Fact]
        public void Evento_DiceSiEsMomentoClave_ComoEnSuPaquete()
        {
            int checkedEvents = 0;
            foreach (ulong seed in new ulong[] { 1, 7, 42 })
            {
                LifePresenter presenter = NewGame(seed);
                for (int step = 0; step < 40 && presenter.Phase != LifePhase.Over && presenter.Phase != LifePhase.ChoosingHeir; step++)
                {
                    if (presenter.Phase == LifePhase.Choosing)
                    {
                        EventView view = presenter.Snapshot().PendingEvent!;
                        Assert.Equal(presenter.Session.Content.GetEvent(view.EventId).KeyDay, view.IsKeyMoment);
                        checkedEvents++;
                        presenter.Choose(0);
                    }
                    else
                    {
                        presenter.AdvanceUntilSomething();
                    }
                }
            }

            Assert.True(checkedEvents > 0);
        }

        private static LifeSnapshot WithStats(LifeSnapshot s, int health, int happiness, int smarts, decimal money) =>
            new LifeSnapshot(s.Phase, s.PlayerName, s.AgeInYears, s.AgeLabel, s.DateLabel, s.Place, health, happiness, smarts, money, s.MoneyLabel,
                s.Generation, s.Seed, s.PendingEvent, s.Family, s.Heirs, s.Home, s.DailyPlace, s.DailyPlaceLabel, s.Workplace, s.People);
    }
}

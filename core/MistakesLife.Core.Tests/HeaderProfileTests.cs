using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D6-1a: la cabecera con tu plata y lo que ganas al mes, la tira de la fecha y tu ficha.</summary>
    public class HeaderProfileTests
    {
        private static LifePresenter NewGame(ulong seed) => new LifePresenter(new LifeSession(RepoPacks.Content, seed));

        private static void PlayUntil(LifePresenter presenter, int years)
        {
            LifeSession session = presenter.Session;
            int target = years * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }
        }

        [Fact]
        public void Tira_DiceLaFechaSinElDiaDeLaSemana_YElAnoDelMundo()
        {
            LifePresenter presenter = NewGame(5);
            presenter.AdvanceMonth();
            HeaderView header = presenter.Header();
            GameDate date = presenter.Session.World.Date;

            Assert.Equal(date.DayOfMonth + " de " + presenter.Text("ui.mes." + date.Month), header.Date);
            Assert.DoesNotContain("lunes", header.Date);
            Assert.Equal("Año " + date.Year, header.Year);
        }

        [Fact]
        public void Cabecera_DeNino_EsEfectivo_YSinIngresos()
        {
            LifePresenter presenter = NewGame(5);
            HeaderView header = presenter.Header();

            Assert.True(header.Cash);
            Assert.Equal(string.Empty, header.Income);
            Assert.Empty(presenter.Income().Lines);
        }

        [Fact]
        public void Cabecera_BajoCero_LlevaElSignoAdelante_YSeMarca()
        {
            LifePresenter presenter = NewGame(5);
            presenter.Session.World.Player.Money = -86400m;
            HeaderView header = presenter.Header();

            Assert.True(header.BelowZero);
            Assert.Equal("-$86.400", header.Money);
        }

        [Fact]
        public void Ingresos_SumanSueldoYPension_YLaCabeceraMuestraElTotal()
        {
            LifePresenter presenter = NewGame(5);
            Character player = presenter.Session.World.Player;
            player.Job = new Job(JobKind.Employee, 698000m, 1);
            player.LastPayslip = new Payslip(presenter.Session.World.Date, JobKind.Employee, 698000m, new List<PayslipLine>(), 698000m);
            player.Pension = 100000m;
            player.LastPensionPayment = 44000m;

            IncomeView income = presenter.Income();

            Assert.Equal(698000m + 44000m, income.Total);
            Assert.Equal(new[] { "ui.dinero.sueldo", "ui.dinero.pension" }, income.Lines.Select(l => l.Id));
            Assert.Equal("+$742.000", presenter.Header().Income);
            // Sin cuenta en el banco, la plata sigue siendo efectivo aunque tenga pega (D6-1b).
            Assert.True(presenter.Header().Cash);
            player.Account = new BankAccount("banco.chile.nacional", presenter.Session.World.Date);
            Assert.False(presenter.Header().Cash);
        }

        [Fact]
        public void Ingresos_SinPega_NoCuentanElUltimoSueldo()
        {
            LifePresenter presenter = NewGame(5);
            Character player = presenter.Session.World.Player;
            player.LastPayslip = new Payslip(presenter.Session.World.Date, JobKind.Employee, 698000m, new List<PayslipLine>(), 698000m);

            Assert.Equal(0m, presenter.Income().Total);
        }

        [Fact]
        public void Ficha_DeNino_TraeSuCursoYSuComuna_SinHijosNiPega()
        {
            LifePresenter presenter = NewGame(5);
            PlayUntil(presenter, 8);
            Character player = presenter.Session.World.Player;
            ProfileView profile = presenter.Profile();

            Assert.Equal(player.FirstName + " " + player.LastName, profile.Name);
            Assert.Contains("naciste el", profile.AgeLine);
            Assert.Contains(profile.Rows, r => r.Id == "colegio");
            Assert.Contains(profile.Rows, r => r.Id == "vive" && r.Value == presenter.Text(player.Residence!.CommuneId));
            Assert.DoesNotContain(profile.Rows, r => r.Id == "hijos" || r.Id == "trabajo");
        }

        [Fact]
        public void Ficha_DeAdulto_TraeSuPega_YDiceSiTieneHijos()
        {
            LifePresenter presenter = NewGame(7);
            PlayUntil(presenter, 26);
            Character player = presenter.Session.World.Player;
            player.Job ??= new Job(JobKind.Employee, 600000m, 1);
            ProfileView profile = presenter.Profile();

            Assert.Contains(profile.Rows, r => r.Id == "trabajo");
            Assert.Contains(profile.Rows, r => r.Id == "hijos");
        }

        [Fact]
        public void Ficha_NingunTextoQuedaComoClaveSinTraducir()
        {
            foreach (ulong seed in new ulong[] { 3, 7, 12 })
            {
                LifePresenter presenter = NewGame(seed);
                foreach (int age in new[] { 7, 20, 32 })
                {
                    PlayUntil(presenter, age);
                    if (!presenter.Session.World.Player.IsAlive)
                    {
                        break;
                    }

                    ProfileView profile = presenter.Profile();
                    HeaderView header = presenter.Header();
                    var texts = profile.Rows.SelectMany(r => new[] { r.Label, r.Value, r.Detail })
                        .Concat(new[] { profile.AgeLine, header.Date, header.Year })
                        .Concat(presenter.Income().Lines.Select(l => l.Label));
                    Assert.DoesNotContain(texts, t => t.StartsWith("ui.", StringComparison.Ordinal) || t.Contains(".titulo"));
                }
            }
        }
    }
}

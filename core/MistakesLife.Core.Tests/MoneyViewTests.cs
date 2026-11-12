using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Tu dinero (docs/diseno-app.md): la cartola muestra solo lo que de verdad se movió.</summary>
    public class MoneyViewTests
    {
        private static LifePresenter NewGame() => new LifePresenter(new LifeSession(RepoPacks.Content, 7));

        [Fact]
        public void TuDinero_AlNacer_NoTieneMovimientosNiDeudas()
        {
            MoneyView money = NewGame().Money();

            Assert.Empty(money.Lines);
            Assert.Empty(money.Debts);
            Assert.Equal(string.Empty, money.SpentLabel);
            Assert.Equal(string.Empty, money.DaysLabel);
            Assert.StartsWith("$", money.BalanceLabel);
        }

        [Fact]
        public void TuDinero_ConGastosDelMes_MuestraCadaGastoConElNombreDeSuDecision()
        {
            LifePresenter presenter = NewGame();
            Character player = presenter.Session.World.Player;
            player.LastExpenses = new[] { new PayslipLine("vivienda.pieza", 150000m), new PayslipLine("servicio.transporte.bus", 34980m) };

            MoneyView money = presenter.Money();

            Assert.Equal(new[] { "Arriendo de la pieza", "Transporte: micro o colectivo" }, money.Lines.Select(l => l.Label));
            Assert.All(money.Lines, l => Assert.False(l.IsIncome));
            Assert.Equal("-$150.000", money.Lines[0].AmountLabel);
            Assert.Equal("-$184.980", money.SpentLabel);

            // Sin sueldo no hay días de sueldo que contar.
            Assert.All(money.Lines, l => Assert.Equal(string.Empty, l.Note));
            Assert.Equal(string.Empty, money.DaysLabel);
        }

        [Fact]
        public void TuDinero_ConSueldo_PrimeroElSueldoYCadaGastoEnDiasDeSueldo()
        {
            LifePresenter presenter = NewGame();
            Character player = presenter.Session.World.Player;
            player.Job = new Job(JobKind.Employee, 600000m, 1);
            player.LastPayslip = new Payslip(GameDate.Start, JobKind.Employee, 600000m, Array.Empty<PayslipLine>(), 600000m);
            player.LastExpenses = new[] { new PayslipLine("vivienda.pieza", 120000m), new PayslipLine("servicio.celular.plan", 5000m) };
            player.LastDebtPayment = 40000m;

            MoneyView money = presenter.Money();

            MoneyLineView salary = money.Lines[0];
            Assert.True(salary.IsIncome);
            Assert.Equal("+$600.000", salary.AmountLabel);

            // 600.000 al mes son 20.000 por día: el arriendo es 6 días y el celular menos de uno.
            Assert.Equal("6 días de sueldo", money.Lines[1].Note);
            Assert.Equal("menos de un día de sueldo", money.Lines[2].Note);
            Assert.Equal("Cuotas de tus deudas", money.Lines[3].Label);
            Assert.Equal("2 días de sueldo", money.Lines[3].Note);
            Assert.Equal("El mes te costó 8 días de sueldo", money.DaysLabel);
        }

        [Fact]
        public void TuDinero_ConDeudaAtrasada_LaMuestraConSuSaldoYSuAtraso()
        {
            LifePresenter presenter = NewGame();
            var debt = new Debt("deuda.chile.linea_credito", 250000m, 12, 1) { MonthsLate = 2 };
            presenter.Session.World.Player.AddDebt(debt);

            MoneyLineView line = Assert.Single(presenter.Money().Debts);

            Assert.Equal("Línea de crédito", line.Label);
            Assert.Equal("$250.000", line.AmountLabel);
            Assert.Equal("cuotas atrasadas: 2", line.Note);
        }
    }
}

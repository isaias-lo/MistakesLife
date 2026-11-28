using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// D6-1b: los bancos de Chile, abrir tu cuenta y la cartola. Cada peso que entra o sale
    /// queda anotado con su causa, y la cartola cuadra con el saldo.
    /// </summary>
    public class BankTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida en Chile que llega a los 24 años, sin pega y viviendo con sus papás.</summary>
        private static LifeSession At24(ulong seed = 5)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (24 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, "jugador.dejar_trabajo()");
            return session;
        }

        /// <summary>Avanza esas semanas, eligiendo la primera opción de cada momento que aparezca.</summary>
        private static void Play(LifeSession session, int weeks)
        {
            int target = session.World.Date.TotalWeeks + weeks;
            while (session.World.Player.IsAlive && session.World.Date.TotalWeeks < target)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(target - session.World.Date.TotalWeeks);
            }
        }

        private static void Run(World world, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static bool Eval(World world, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, Content, world.Player, null, readOnly: true)).AsBool(expression);

        [Fact]
        public void Chile_TieneTresBancos_ConSusCuentas()
        {
            BanksDefinition banks = Content.GetCountry("pais.chile").Economy!.Banks!;

            Assert.Equal(18, banks.MinAge);
            Assert.Equal(new[] { "banco.chile.nacional", "banco.chile.pacifico", "banco.chile.tapp" }, banks.Banks.Select(b => b.Id));
            Assert.True(banks.Bank("banco.chile.pacifico")!.NeedsContract);
            Assert.Equal(0, banks.Bank("banco.chile.nacional")!.MonthlyFee);
        }

        [Fact]
        public void AbrirCuenta_EnElNacional_LaPlataQueTeniaEsElPrimerMovimiento_YYaNoEsEfectivo()
        {
            LifeSession session = At24();
            World world = session.World;
            var presenter = new LifePresenter(session);
            world.Player.Money = 150000;
            Assert.True(presenter.Header().Cash);
            Assert.False(Eval(world, "jugador.tiene_cuenta"));

            Run(world, "jugador.abrir_cuenta('banco.chile.nacional')");

            Assert.True(Eval(world, "jugador.tiene_cuenta"));
            Assert.Equal("banco.chile.nacional", world.Player.Account!.BankId);
            BankMovement first = Assert.Single(world.Player.Account.Movements);
            Assert.Equal(BankRules.OpeningDeposit, first.Id);
            Assert.Equal(150000, first.Amount);
            Assert.Equal(150000, world.Player.Money);
            Assert.False(presenter.Header().Cash);
            Assert.Contains(world.Journal, j => j.TextKey == "diario.chile.banco.cuenta_abierta");
        }

        [Fact]
        public void BancoPacifico_SinContrato_DiceQueNo_YQuedaElMotivoEnElDiario()
        {
            LifeSession session = At24();
            World world = session.World;
            Assert.False(Eval(world, "jugador.puede_abrir_cuenta('banco.chile.pacifico')"));

            Run(world, "jugador.abrir_cuenta('banco.chile.pacifico')");

            Assert.Null(world.Player.Account);
            Assert.Contains(world.Journal, j => j.TextKey == "diario.chile.banco.rechazo_contrato");
            Assert.True(Eval(world, "jugador.puede_abrir_cuenta('banco.chile.tapp')"));
        }

        [Fact]
        public void BancoPacifico_ConContratoPeroSueldoBajo_DiceQueNoPorLaRenta()
        {
            LifeSession session = At24();
            World world = session.World;
            Character player = world.Player;
            // Medio sueldo de lo que pide el banco, a los precios de hoy.
            decimal half = PayrollCalculator.ToToday(300000m, world.EconomyOf("pais.chile")!.PriceIndex);
            player.Job = new Job(JobKind.Employee, half, 1);
            player.LastPayslip = new Payslip(world.Date, JobKind.Employee, half, new System.Collections.Generic.List<PayslipLine>(), half);

            Run(world, "jugador.abrir_cuenta('banco.chile.pacifico')");

            Assert.Null(player.Account);
            Assert.Contains(world.Journal, j => j.TextKey == "diario.chile.banco.rechazo_renta");
        }

        [Fact]
        public void ConCuenta_CadaPesoQueEntraOSale_QuedaEnLaCartola_YCuadraConElSaldo()
        {
            LifeSession session = At24();
            World world = session.World;
            world.Player.Money = 2_000_000;
            Run(world, "jugador.abrir_cuenta('banco.chile.nacional')");
            Run(world, "jugador.trabajar(900000)");

            Play(session, 14);

            Character player = world.Player;
            Assert.Equal(player.Money, player.Account!.Movements.Sum(m => m.Amount));
            Assert.Contains(player.Account.Movements, m => m.Id == "movimiento.remuneracion" && m.Amount > 0);
        }

        [Fact]
        public void BancoPacifico_CobraSuMantencionCadaMes_YSeVeEnLosGastos()
        {
            LifeSession session = At24();
            World world = session.World;
            world.Player.Money = 1_000_000;
            world.Player.Account = new BankAccount("banco.chile.pacifico", world.Date);

            Play(session, 5);

            BankMovement fee = Assert.Single(world.Player.Account.Movements, m => m.Id == BankRules.MonthlyFeeMovement);
            Assert.Equal(-PayrollCalculator.ToToday(7900m, world.EconomyOf("pais.chile")!.PriceIndex), fee.Amount);
            Assert.Equal("banco.chile.pacifico", fee.Detail);
            Assert.Contains(world.Player.LastExpenses, l => l.Id == BankRules.MonthlyFeeMovement);
        }

        [Fact]
        public void Cartola_MuestraElMes_ConCadaMovimientoSuFechaYDeQuien()
        {
            LifeSession session = At24();
            World world = session.World;
            world.Player.Money = 500000;
            Run(world, "jugador.abrir_cuenta('banco.chile.tapp')");
            Run(world, "jugador.endeudarse('deuda.chile.consumo', 300000)");
            var presenter = new LifePresenter(session);

            BankView bank = presenter.Bank()!;

            Assert.Equal("Tapp", bank.BankName);
            Assert.Equal("Cuenta vista", bank.AccountLabel);
            BankMonthView month = Assert.Single(bank.Months);
            Assert.StartsWith("+$", month.InLabel);
            Assert.Equal(string.Empty, month.OutLabel);
            Assert.Equal("Préstamo · Crédito de consumo", month.Lines[0].Label);
            Assert.Equal("Depósito al abrir la cuenta", month.Lines[1].Label);
            Assert.Equal(world.Date.DayOfMonth + " de " + presenter.Text("ui.mes." + world.Date.Month), month.Lines[0].Note);
        }

        [Fact]
        public void SinCuenta_NoHayBanco_YAbrirlaEsUnBotonDeTuDinero()
        {
            LifeSession session = At24();
            var presenter = new LifePresenter(session);

            Assert.Null(presenter.Bank());
            ActionView open = Assert.Single(presenter.MoneyActions(), a => a.EventId == "evento.banco.abrir_cuenta");
            Assert.Equal("Abrir tu cuenta en el banco", open.Label);

            Run(session.World, "jugador.abrir_cuenta('banco.chile.nacional')");

            Assert.DoesNotContain(presenter.MoneyActions(), a => a.EventId == "evento.banco.abrir_cuenta");
        }

        [Fact]
        public void AvisosDelBanco_TraenLoQueLlego_YNoSeRepiten()
        {
            LifeSession session = At24();
            World world = session.World;
            world.Player.Money = 100000;
            Run(world, "jugador.abrir_cuenta('banco.chile.nacional')");
            var presenter = new LifePresenter(session);
            Assert.Empty(presenter.TakeBankNotices());

            Run(world, "jugador.trabajar(900000)");
            Play(session, 9);

            Assert.Contains(presenter.TakeBankNotices(), n => n.Id == "movimiento.remuneracion");
            Assert.Empty(presenter.TakeBankNotices());
        }

        [Fact]
        public void AvisosDelBanco_PreguntandoCadaSemana_TraenElSueldo()
        {
            // Así los pide el juego: una vez por semana, después de avanzar.
            LifeSession session = At24();
            World world = session.World;
            world.Player.Money = 100000;
            Run(world, "jugador.abrir_cuenta('banco.chile.nacional')");
            var presenter = new LifePresenter(session);
            presenter.TakeBankNotices();
            Run(world, "jugador.trabajar(900000)");

            var seen = new System.Collections.Generic.List<MoneyLineView>();
            for (int week = 0; week < 9; week++)
            {
                Play(session, 1);
                seen.AddRange(presenter.TakeBankNotices());
            }

            Assert.Contains(seen, n => n.Id == "movimiento.remuneracion");
            int paid = world.Player.Account!.Movements.Count(m => m.Id == "movimiento.remuneracion");
            Assert.Equal(paid, seen.Count(n => n.Id == "movimiento.remuneracion"));
        }

        [Fact]
        public void GuardarYCargar_ConservaLaCuentaYLaCartola()
        {
            LifeSession session = At24();
            session.World.Player.Money = 300000;
            Run(session.World, "jugador.abrir_cuenta('banco.chile.nacional')");
            Run(session.World, "jugador.endeudarse('deuda.chile.consumo', 200000)");

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());

            BankAccount account = loaded.World.Player.Account!;
            Assert.Equal("banco.chile.nacional", account.BankId);
            Assert.Equal(session.World.Player.Account!.Movements.Select(m => (m.Id, m.Amount, m.Detail, m.Date)), account.Movements.Select(m => (m.Id, m.Amount, m.Detail, m.Date)));
        }

        [Fact]
        public void Cartola_GuardaSoloDosAnos()
        {
            var account = new BankAccount("banco.chile.nacional", GameDate.Start);
            account.Add(new BankMovement(GameDate.Start, "movimiento.otro", 1, ""));
            account.Add(new BankMovement(GameDate.Start.AddWeeks(BankAccount.KeptWeeks + 1), "movimiento.otro", 2, ""));

            Assert.Equal(2, Assert.Single(account.Movements).Amount);
        }

        [Fact]
        public void PantallaDelBanco_MuestraLosTresBancos_YDiceConPalabrasPorQueUnoNoTeAbre()
        {
            LifeSession session = At24();
            var presenter = new LifePresenter(session);

            var offers = presenter.BankOffers();

            Assert.Equal(new[] { "Banco Nacional", "Banco Pacífico", "Tapp" }, offers.Select(o => o.Name));
            Assert.Equal(new[] { "BN", "BP", "T" }, offers.Select(o => o.Initials));
            Assert.All(offers, o => Assert.Equal(3, o.Lines.Count));
            BankOfferView pacific = offers[1];
            Assert.Equal("Grande", pacific.KindLabel);
            Assert.False(pacific.CanOpen);
            Assert.Equal("Pide un trabajo con contrato", pacific.Reason);
            Assert.True(offers[0].CanOpen);
            Assert.Equal(string.Empty, offers[0].Reason);
            Assert.True(Assert.Single(presenter.MoneyActions(), a => a.EventId == "evento.banco.abrir_cuenta").OpensBank);
        }

        [Fact]
        public void AbrirDesdeLaPantalla_ElBancoContestaAlTiroConLoQueCambia()
        {
            LifeSession session = At24();
            session.World.Player.Money = 38000;
            var presenter = new LifePresenter(session);

            BankAnswerView answer = presenter.OpenAccount("banco.chile.nacional");

            Assert.True(answer.Opened);
            Assert.Equal("Tu cuenta vista está lista", answer.Title);
            Assert.Equal("Se depositaron tus $38.000 en efectivo. No cobra mantención. Te llega la tarjeta de débito.", answer.Message);
            Assert.Empty(presenter.BankOffers());
            Assert.NotNull(presenter.Bank());
        }

        [Fact]
        public void AbrirDesdeLaPantalla_SinCumplir_ElBancoDiceQueNoYPorQue()
        {
            LifeSession session = At24();
            var presenter = new LifePresenter(session);

            BankAnswerView answer = presenter.OpenAccount("banco.chile.pacifico");

            Assert.False(answer.Opened);
            Assert.Equal("El banco te dijo que no", answer.Title);
            Assert.Equal("Pide un trabajo con contrato", answer.Message);
            Assert.Null(session.World.Player.Account);
        }

        [Fact]
        public void LoQueTeEntraEnElAno_SumaElSueldo_SinElEfectivoDelPrincipio()
        {
            LifeSession session = At24();
            World world = session.World;
            world.Player.Money = 2_000_000;
            Run(world, "jugador.abrir_cuenta('banco.chile.nacional')");
            Run(world, "jugador.trabajar(900000)");
            Play(session, 14);

            BankView bank = new LifePresenter(session).Bank()!;

            Assert.Contains(bank.YearIncome, l => l.Id == "movimiento.remuneracion");
            Assert.DoesNotContain(bank.YearIncome, l => l.Id == BankRules.OpeningDeposit);
            Assert.StartsWith("+$", bank.YearIncomeLabel);
        }
    }
}

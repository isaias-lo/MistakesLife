using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas, después de las pensiones: el seguro que paga al perder un
    /// trabajo con contrato, con las reglas del país ("economia.seguro_desempleo"; en
    /// Chile, el Seguro de Cesantía). El jugador no tiene que hacer nada: se cobra solo.
    /// - Cada mes con sueldo con contrato, su descuento y el aporte del empleador se
    ///   ahorran en su cuenta (en dinero de los paquetes, así la inflación no lo achica),
    ///   suma una cotización y su sueldo entra al promedio.
    /// - La semana después de quedar sin ese trabajo nace su cobro. Si no tenía las
    ///   cotizaciones mínimas, no hay pagos (y el diario lo dice).
    /// - Desde el mes siguiente, al empezar cada mes, se deposita un porcentaje del sueldo
    ///   promedio que baja mes a mes. Lo paga su cuenta mientras tenga saldo; si no
    ///   alcanza y el despido no fue culpa suya, completa el fondo común con su mínimo y
    ///   su máximo. El primer pago y el último se anotan en el diario (los correos del
    ///   país los escuchan) y cada pago queda guardado con su número.
    /// - Con un trabajo nuevo el cobro termina, y las cotizaciones se cuentan de nuevo
    ///   desde el último pago. Al jubilarse, lo que quedaba en la cuenta se retira de una vez.
    /// </summary>
    public sealed class UnemploymentInsuranceSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.seguro_desempleo";

        private readonly ContentDatabase _content;

        public UnemploymentInsuranceSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Residence != null).OrderBy(c => c.Id))
            {
                EconomyDefinition? economyRules = _content.GetCountry(character.Residence!.CountryId).Economy;
                UnemploymentInsuranceDefinition? rules = economyRules?.UnemploymentInsurance;
                CountryEconomy? economy = world.EconomyOf(character.Residence.CountryId);
                if (rules == null || economy == null)
                {
                    continue;
                }

                bool player = character.Id == world.PlayerId;
                if (context.Date.IsFirstWeekOfMonth)
                {
                    Save(character, rules, economyRules!, economy.PriceIndex, context);
                }

                if (character.Pension.HasValue)
                {
                    WithdrawAtRetirement(world, character, rules, economy.PriceIndex, player);
                    continue;
                }

                if (character.Job != null)
                {
                    character.UnemploymentClaim = null;
                    continue;
                }

                if (character.UnemploymentClaim == null)
                {
                    StartClaim(world, character, rules, context, player);
                    continue;
                }

                UnemploymentClaim claim = character.UnemploymentClaim;
                if (!claim.Finished && context.Date.IsFirstWeekOfMonth && context.Date > claim.Start)
                {
                    Pay(world, character, claim, rules, economy.PriceIndex, context, player);
                }
            }
        }

        /// <summary>Un mes con sueldo con contrato: ahorra en la cuenta, suma una cotización y entra al promedio.</summary>
        private static void Save(Character character, UnemploymentInsuranceDefinition rules, EconomyDefinition economyRules, double priceIndex, WeekContext context)
        {
            Payslip? payslip = character.LastPayslip;
            if (payslip == null || !payslip.Date.Equals(context.Date) || payslip.Kind != JobKind.Employee)
            {
                return;
            }

            decimal worker = payslip.Amount(rules.Deduction);
            double workerRate = economyRules.Payroll.Employee.FirstOrDefault(d => d.Id == rules.Deduction)?.Rate ?? 0;
            // El aporte del empleador se calcula sobre el mismo sueldo (con el mismo tope) que el descuento.
            decimal employer = workerRate > 0 ? worker / (decimal)workerRate * (decimal)rules.EmployerRate : 0;
            character.UnemploymentFund += (worker + employer) / (decimal)priceIndex;
            character.UnemploymentContributions++;
            decimal realGross = payslip.Gross / (decimal)priceIndex;
            character.UnemploymentSalaryBase = character.UnemploymentSalaryBase <= 0
                ? realGross
                : character.UnemploymentSalaryBase + ((realGross - character.UnemploymentSalaryBase) / rules.AverageMonths);
        }

        /// <summary>Quedó sin un trabajo con contrato: nace su cobro, con o sin derecho a pagos.</summary>
        private static void StartClaim(World world, Character character, UnemploymentInsuranceDefinition rules, WeekContext context, bool player)
        {
            Job? lastJob = character.LastJob;
            if (lastJob == null || lastJob.Kind != JobKind.Employee)
            {
                return;
            }

            bool hasRight = character.UnemploymentContributions >= rules.MinimumContributions;
            SolidarityFundDefinition? fund = rules.SolidarityFund;
            bool solidarity = hasRight && fund != null && fund.Flags.Any(character.HasFlag) && lastJob.MonthsInJob >= fund.MonthsWithLastEmployer;
            character.UnemploymentClaim = new UnemploymentClaim(context.Date, hasRight, solidarity, character.UnemploymentSalaryBase);
            if (!hasRight && character.UnemploymentContributions > 0 && player)
            {
                world.WriteJournal(character.Id, rules.NoRightJournal);
            }
        }

        /// <summary>El pago de este mes: de la cuenta mientras alcance; si no, del fondo común si corresponde.</summary>
        private static void Pay(World world, Character character, UnemploymentClaim claim, UnemploymentInsuranceDefinition rules, double priceIndex, WeekContext context, bool player)
        {
            int index = claim.Payments.Count;
            double share = PaymentShare(rules, index);
            decimal amount = PayrollCalculator.ToToday(claim.SalaryBase * (decimal)share, priceIndex);
            decimal inAccount = PayrollCalculator.ToToday(character.UnemploymentFund, priceIndex);
            SolidarityFundDefinition? fund = rules.SolidarityFund;
            decimal fromAccount;
            decimal fromFund = 0;
            if (amount > 0 && inAccount >= amount)
            {
                fromAccount = amount;
            }
            else if (FundCovers(claim, fund, index))
            {
                decimal scale = (decimal)(share / rules.Payments[0]);
                decimal minimum = PayrollCalculator.ToToday(fund!.FirstMinimum * scale, priceIndex);
                decimal maximum = PayrollCalculator.ToToday(fund.FirstMaximum * scale, priceIndex);
                decimal target = Math.Min(Math.Max(amount, minimum), maximum);
                fromAccount = Math.Min(inAccount, target);
                fromFund = target - fromAccount;
            }
            else
            {
                fromAccount = inAccount;
            }

            decimal paid = fromAccount + fromFund;
            if (paid <= 0)
            {
                claim.Finished = true;
                return;
            }

            character.Move(world.Date, "movimiento.seguro_cesantia", paid);
            character.UnemploymentFund = fromAccount >= inAccount ? 0 : character.UnemploymentFund - (fromAccount / (decimal)priceIndex);
            claim.Add(new UnemploymentPayment(context.Date, index + 1, paid, fromFund));
            if (index == 0)
            {
                // Las cotizaciones para volver a cobrar se cuentan desde el último pago.
                character.UnemploymentContributions = 0;
                if (player)
                {
                    world.WriteJournal(character.Id, rules.FirstPaymentJournal);
                }
            }

            bool moreFromFund = FundCovers(claim, fund, index + 1);
            if (character.UnemploymentFund <= 0 && !moreFromFund)
            {
                claim.Finished = true;
                if (player && index > 0)
                {
                    world.WriteJournal(character.Id, rules.LastPaymentJournal);
                }
            }
        }

        /// <summary>Al jubilarse ya no hay cobro, y lo que quedaba en la cuenta se retira de una vez.</summary>
        private static void WithdrawAtRetirement(World world, Character character, UnemploymentInsuranceDefinition rules, double priceIndex, bool player)
        {
            character.UnemploymentClaim = null;
            if (character.UnemploymentFund <= 0)
            {
                return;
            }

            character.Move(world.Date, "movimiento.retiro_seguro_cesantia", PayrollCalculator.ToToday(character.UnemploymentFund, priceIndex));
            character.UnemploymentFund = 0;
            if (player)
            {
                world.WriteJournal(character.Id, rules.RetirementJournal);
            }
        }

        /// <summary>La parte del sueldo promedio del pago número <paramref name="index"/> (desde 0): después del último, el último.</summary>
        public static double PaymentShare(UnemploymentInsuranceDefinition rules, int index) =>
            rules.Payments[Math.Min(index, rules.Payments.Count - 1)];

        private static bool FundCovers(UnemploymentClaim claim, SolidarityFundDefinition? fund, int index) =>
            claim.Solidarity && fund != null && index < fund.Payments;
    }
}

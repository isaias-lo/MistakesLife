using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas, después de los gastos: las deudas, con las reglas del país
    /// ("economia.deudas"). Al empezar cada mes, cada deuda:
    /// - se reajusta con la inflación si es reajustable, y suma su interés;
    /// - si se cumple su condición de pago, cobra la cuota (el interés y una parte
    ///   de lo que se debe). Si no hay dinero suficiente, la cuota queda sin pagar
    ///   y la deuda sigue creciendo.
    /// Las deudas rotativas (tarjetas) cobran un pago mínimo en vez de cuotas fijas.
    /// Las que se pagan con el saldo (línea de crédito) se llevan todo el dinero
    /// que haya, hasta lo que se debe.
    /// Con varios meses seguidos sin pagar, la persona queda en el registro de
    /// morosos; sale al ponerse al día. Si el tipo lo dice, tras unos meses sin pagar
    /// el banco la castiga: deja de sumar intereses (en Chile, a los 6 meses). Si el
    /// país lo dice, tras muchos meses sin pagar la deuda prescribe y se borra.
    /// </summary>
    public sealed class DebtSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.deudas";

        public const string PaidOffJournal = "diario.deudas.pagada";
        public const string DelinquentJournal = "diario.deudas.moroso";
        public const string ClearedJournal = "diario.deudas.fuera_registro";
        public const string PrescribedJournal = "diario.deudas.prescrita";
        public const string WrittenOffJournal = "diario.deudas.castigada";

        private readonly ContentDatabase _content;

        public DebtSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            if (!context.Date.IsFirstWeekOfMonth)
            {
                return;
            }

            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Residence != null && c.Debts.Count > 0).OrderBy(c => c.Id))
            {
                DebtsDefinition? rules = _content.GetCountry(character.Residence!.CountryId).Economy?.Debts;
                CountryEconomy? economy = world.EconomyOf(character.Residence.CountryId);
                if (rules == null || economy == null)
                {
                    continue;
                }

                var scope = new EventScope(world, _content, character, null, readOnly: true);
                decimal paid = 0;
                foreach (Debt debt in character.Debts.ToList())
                {
                    DebtTypeDefinition type = rules.Type(debt.TypeId) ?? throw new InvalidOperationException($"{character.Residence.CountryId} no tiene la deuda {debt.TypeId}.");
                    paid += Advance(world.Date, debt, type, character, scope, economy.PriceIndex);
                    if (debt.Balance > 0 && debt.MonthsLate == type.WriteOffMonths)
                    {
                        world.WriteJournal(character.Id, WrittenOffJournal);
                    }

                    if (debt.Balance <= 0)
                    {
                        character.RemoveDebt(debt);
                        world.WriteJournal(character.Id, PaidOffJournal);
                    }
                    else if (rules.MonthsToPrescription.HasValue && debt.MonthsLate >= rules.MonthsToPrescription.Value)
                    {
                        character.RemoveDebt(debt);
                        world.WriteJournal(character.Id, PrescribedJournal);
                    }
                }

                character.LastDebtPayment = paid;
                bool wasDelinquent = character.Delinquent;
                character.Delinquent = character.Debts.Any(d => d.MonthsLate >= rules.MonthsToDelinquent)
                    || (wasDelinquent && character.Debts.Any(d => d.MonthsLate > 0));
                if (character.Delinquent && !wasDelinquent)
                {
                    world.WriteJournal(character.Id, DelinquentJournal);
                }
                else if (!character.Delinquent && wasDelinquent)
                {
                    world.WriteJournal(character.Id, ClearedJournal);
                }
            }
        }

        /// <summary>Un mes de una deuda. Devuelve lo que se pagó.</summary>
        private static decimal Advance(GameDate date, Debt debt, DebtTypeDefinition type, Character character, EventScope scope, double priceIndex)
        {
            if (!IsWrittenOff(debt, type))
            {
                if (type.Indexed && debt.PriceIndex > 0)
                {
                    debt.Balance *= (decimal)(priceIndex / debt.PriceIndex);
                }

                debt.Balance = Round(debt.Balance * (1 + (decimal)MonthlyRate(type.AnnualRate)));
            }

            debt.PriceIndex = priceIndex;
            if (type.CompiledPaymentCondition != null && !type.CompiledPaymentCondition.IsTrue(scope))
            {
                return 0;
            }

            decimal installment = type.MinimumPaymentRate.HasValue
                ? MinimumPayment(debt.Balance, type, priceIndex)
                : Installment(debt.Balance, type.AnnualRate, debt.RemainingInstallments);
            if (type.PaysFromBalance)
            {
                // El banco se lleva todo lo que haya, aunque no alcance al mínimo.
                decimal swept = Math.Min(character.Money, debt.Balance);
                character.Move(date, "movimiento.pago_deuda", -swept, debt.TypeId);
                debt.Balance -= swept;
                debt.MonthsLate = swept < installment ? debt.MonthsLate + 1 : 0;
                return swept;
            }

            if (character.Money < installment)
            {
                debt.MonthsLate++;
                return 0;
            }

            character.Move(date, "movimiento.pago_deuda", -installment, debt.TypeId);
            debt.Balance -= installment;
            debt.RemainingInstallments = Math.Max(1, debt.RemainingInstallments - 1);
            debt.MonthsLate = 0;
            return installment;
        }

        /// <summary>
        /// Si el banco ya castigó la deuda (lleva "meses_para_castigo" meses seguidos
        /// sin pagar): no suma intereses ni se le presta más.
        /// </summary>
        public static bool IsWrittenOff(Debt debt, DebtTypeDefinition type) =>
            type.WriteOffMonths.HasValue && debt.MonthsLate >= type.WriteOffMonths.Value;

        /// <summary>La tasa de interés de un mes, a partir de la anual.</summary>
        public static double MonthlyRate(double annualRate) => Math.Pow(1 + annualRate, 1.0 / 12) - 1;

        /// <summary>
        /// La cuota fija que paga la deuda en las cuotas que quedan (con la última,
        /// todo lo que se debe).
        /// </summary>
        public static decimal Installment(decimal balance, double annualRate, int remaining)
        {
            if (remaining <= 1)
            {
                return balance;
            }

            double rate = MonthlyRate(annualRate);
            if (rate <= 0)
            {
                return Round(balance / remaining);
            }

            double factor = rate / (1 - Math.Pow(1 + rate, -remaining));
            return Math.Min(balance, Round(balance * (decimal)factor));
        }

        /// <summary>
        /// El pago mínimo de una deuda rotativa: una parte de lo que se debe, con un
        /// monto mínimo (o todo, si se debe menos que ese mínimo).
        /// </summary>
        public static decimal MinimumPayment(decimal balance, DebtTypeDefinition type, double priceIndex)
        {
            decimal byRate = Round(balance * (decimal)(type.MinimumPaymentRate ?? 1));
            decimal floor = PayrollCalculator.ToToday(type.MinimumPaymentAmount, priceIndex);
            return Math.Min(balance, Math.Max(byRate, floor));
        }

        /// <summary>
        /// Repactar: las deudas atrasadas que se pueden repactar quedan al día, en
        /// nuevas cuotas (con su interés de siempre). Devuelve cuántas repactó.
        /// Al ponerse al día, sale del registro de morosos el mes siguiente.
        /// Si el país pide trabajo para repactar ("repactar_con_trabajo") y no tiene, no repacta.
        /// </summary>
        public static int Reschedule(Character character, DebtsDefinition rules)
        {
            if (rules.RescheduleNeedsJob && character.Job == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Debt debt in character.Debts.Where(d => d.MonthsLate > 0))
            {
                DebtTypeDefinition? type = rules.Type(debt.TypeId);
                if (type?.RescheduleInstallments == null)
                {
                    continue;
                }

                debt.MonthsLate = 0;
                debt.RemainingInstallments = type.RescheduleInstallments.Value;
                count++;
            }

            return count;
        }

        /// <summary>Si tiene alguna deuda atrasada que se pueda repactar (y trabajo, si el país lo pide).</summary>
        public static bool CanReschedule(Character character, DebtsDefinition? rules) =>
            rules != null && (!rules.RescheduleNeedsJob || character.Job != null) && character.Debts.Any(d => d.MonthsLate > 0 && rules.Type(d.TypeId)?.RescheduleInstallments != null);

        /// <summary>
        /// Endeudarse: suma el monto (a precios de hoy) a la deuda de ese tipo, o
        /// abre una nueva, con todas sus cuotas. Si el tipo entrega dinero, llega a su saldo.
        /// </summary>
        public static void Borrow(GameDate date, Character character, DebtTypeDefinition type, decimal amount, double priceIndex)
        {
            Debt? debt = character.Debts.FirstOrDefault(d => d.TypeId == type.Id);
            if (debt == null)
            {
                character.AddDebt(new Debt(type.Id, amount, type.Installments, priceIndex));
            }
            else
            {
                debt.Balance += amount;
                debt.RemainingInstallments = type.Installments;
            }

            if (type.GivesMoney)
            {
                character.Move(date, "movimiento.prestamo", amount, type.Id);
            }
        }

        private static decimal Round(decimal amount) => Math.Round(amount, MidpointRounding.AwayFromZero);
    }
}

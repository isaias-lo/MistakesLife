using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Simulation;
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
    /// Con varios meses seguidos sin pagar, la persona queda en el registro de
    /// morosos; sale al ponerse al día. Si el país lo dice, tras muchos meses sin
    /// pagar la deuda prescribe y se borra.
    /// </summary>
    public sealed class DebtSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.deudas";

        public const string PaidOffJournal = "diario.deudas.pagada";
        public const string DelinquentJournal = "diario.deudas.moroso";
        public const string ClearedJournal = "diario.deudas.fuera_registro";
        public const string PrescribedJournal = "diario.deudas.prescrita";

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
                    paid += Advance(debt, type, character, scope, economy.PriceIndex);
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
        private static decimal Advance(Debt debt, DebtTypeDefinition type, Character character, EventScope scope, double priceIndex)
        {
            if (type.Indexed && debt.PriceIndex > 0)
            {
                debt.Balance *= (decimal)(priceIndex / debt.PriceIndex);
            }

            debt.PriceIndex = priceIndex;
            debt.Balance = Round(debt.Balance * (1 + (decimal)MonthlyRate(type.AnnualRate)));
            if (type.CompiledPaymentCondition != null && !type.CompiledPaymentCondition.IsTrue(scope))
            {
                return 0;
            }

            decimal installment = Installment(debt.Balance, type.AnnualRate, debt.RemainingInstallments);
            if (character.Money < installment)
            {
                debt.MonthsLate++;
                return 0;
            }

            character.Money -= installment;
            debt.Balance -= installment;
            debt.RemainingInstallments = Math.Max(1, debt.RemainingInstallments - 1);
            debt.MonthsLate = 0;
            return installment;
        }

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
        /// Endeudarse: suma el monto (a precios de hoy) a la deuda de ese tipo, o
        /// abre una nueva, con todas sus cuotas. Si el tipo entrega dinero, llega a su saldo.
        /// </summary>
        public static void Borrow(Character character, DebtTypeDefinition type, decimal amount, double priceIndex)
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
                character.Money += amount;
            }
        }

        private static decimal Round(decimal amount) => Math.Round(amount, MidpointRounding.AwayFromZero);
    }
}

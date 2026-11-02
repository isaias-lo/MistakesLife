using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas: la declaración anual de impuestos, con las reglas del
    /// país ("economia.declaracion_anual").
    /// - Al empezar el mes de declarar, se calcula la declaración de lo ganado el
    ///   año anterior por quien tuvo honorarios (quien solo tuvo contrato ya pagó
    ///   cada mes y no necesita declarar). El jugador la tiene pendiente y la
    ///   acepta con un evento (jugador.declarar()); los demás personajes declaran solos.
    /// - Si el plazo termina (empieza el mes siguiente) y el jugador no declaró:
    ///   si tenía que pagar, paga con multa; si le tocaba devolución, la pierde.
    /// </summary>
    public sealed class TaxSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.impuestos";

        public const string RefundJournal = "diario.impuestos.devolucion";
        public const string PaymentJournal = "diario.impuestos.pago";
        public const string PenaltyJournal = "diario.impuestos.multa";
        public const string LostRefundJournal = "diario.impuestos.devolucion_perdida";

        private readonly ContentDatabase _content;

        public TaxSystem(ContentDatabase content)
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

            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Residence != null).OrderBy(c => c.Id))
            {
                AnnualTaxDefinition? rules = _content.GetCountry(character.Residence!.CountryId).Economy?.AnnualTax;
                CountryEconomy? economy = world.EconomyOf(character.Residence.CountryId);
                if (rules == null || economy == null)
                {
                    continue;
                }

                int month = context.Date.Month;
                if (character.PendingTaxReturn != null && month == (rules.Month % 12) + 1)
                {
                    MissDeadline(world, character, rules);
                }

                if (month == rules.Month)
                {
                    Declare(world, character, rules, economy, context.Date.Year - 1);
                }
            }
        }

        /// <summary>El jugador acepta su declaración pendiente: recibe la devolución o paga.</summary>
        public static void Accept(World world, Character character)
        {
            TaxReturn pending = character.PendingTaxReturn ?? throw new InvalidOperationException($"{character} no tiene una declaración pendiente.");
            character.PendingTaxReturn = null;
            character.Money += pending.Balance;
            if (pending.Balance > 0)
            {
                world.WriteJournal(character.Id, RefundJournal);
            }
            else if (pending.Balance < 0)
            {
                world.WriteJournal(character.Id, PaymentJournal);
            }
        }

        private static void Declare(World world, Character character, AnnualTaxDefinition rules, CountryEconomy economy, int year)
        {
            AnnualIncome? income = character.IncomeOf(year);
            character.ForgetIncomeUntil(year);
            if (income == null || income.FreelanceGross == 0)
            {
                return;
            }

            character.PendingTaxReturn = TaxReturnCalculator.Compute(income, rules, economy.PriceIndex);
            if (character.Id != world.PlayerId)
            {
                Accept(world, character);
            }
        }

        private static void MissDeadline(World world, Character character, AnnualTaxDefinition rules)
        {
            TaxReturn pending = character.PendingTaxReturn!;
            character.PendingTaxReturn = null;
            if (pending.Balance < 0)
            {
                decimal owed = -pending.Balance;
                character.Money -= Math.Round(owed * (1 + (decimal)rules.LatePenalty), MidpointRounding.AwayFromZero);
                world.WriteJournal(character.Id, PenaltyJournal);
            }
            else if (pending.Balance > 0)
            {
                world.WriteJournal(character.Id, LostRefundJournal);
            }
        }
    }
}

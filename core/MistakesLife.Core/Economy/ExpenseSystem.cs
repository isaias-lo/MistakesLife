using System;
using System.Collections.Generic;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas, después de los sueldos: al empezar cada mes el jugador
    /// paga los gastos de su país ("economia.gastos") que le corresponden según
    /// su vida (con condiciones del lenguaje). Los gastos compartidos se dividen
    /// con la pareja. Si el dinero queda en negativo, es un mes "en rojo": baja la
    /// felicidad (estrés financiero) y los eventos pueden reaccionar.
    /// Por ahora solo el jugador lleva sus cuentas; los demás personajes no.
    /// </summary>
    public sealed class ExpenseSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.gastos";

        private readonly ContentDatabase _content;

        public ExpenseSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            if (!context.Date.IsFirstWeekOfMonth || !world.TryGetCharacter(world.PlayerId, out Character? player) || !player.IsAlive || player.Residence == null)
            {
                return;
            }

            ExpensesDefinition? rules = _content.GetCountry(player.Residence.CountryId).Economy?.Expenses;
            CountryEconomy? economy = world.EconomyOf(player.Residence.CountryId);
            if (rules == null || economy == null || player.AgeInYears(context.Date) < rules.FromAge)
            {
                return;
            }

            var lines = new List<PayslipLine>();
            var scope = new EventScope(world, _content, player, null, readOnly: true);
            bool hasPartner = player.PartnerId.HasValue && world.TryGetCharacter(player.PartnerId.Value, out Character? partner) && partner.IsAlive;
            foreach (ExpenseDefinition expense in rules.Categories)
            {
                if (expense.CompiledConditions != null && !expense.CompiledConditions.IsTrue(scope))
                {
                    continue;
                }

                double amount = expense.CompiledAmount!.Evaluate(scope).AsNumber("monto de " + expense.Id);
                if (expense.Shared && hasPartner)
                {
                    amount /= 2;
                }

                decimal today = PayrollCalculator.ToToday((decimal)Math.Max(0, amount), economy.PriceIndex);
                if (today > 0)
                {
                    lines.Add(new PayslipLine(expense.Id, today));
                    player.Money -= today;
                }
            }

            player.LastExpenses = lines;
            if (player.Money < 0)
            {
                player.MonthsInRed++;
                player.Stats.Happiness -= rules.HappinessInRed;
            }
            else
            {
                player.MonthsInRed = 0;
            }
        }
    }
}

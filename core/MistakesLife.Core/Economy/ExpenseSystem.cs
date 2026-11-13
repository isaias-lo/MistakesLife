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
    /// Fase de sistemas, después de los sueldos: al empezar cada mes el jugador
    /// paga el arriendo del contrato que firmó (si arrienda), la compra de comida
    /// que eligió (si vive por su cuenta), los servicios que contrató y los gastos de su
    /// país ("economia.gastos") que le corresponden según su vida (con condiciones
    /// del lenguaje; algunos son plata que recibe, como una pensión de alimentos).
    /// Los gastos compartidos se dividen
    /// con la pareja. Si el dinero no alcanza, es un mes "en rojo": baja la
    /// felicidad (estrés financiero) y los eventos pueden reaccionar. Lo que falta
    /// lo cubre la deuda del país para eso (la línea de crédito, con su interés):
    /// así el saldo nunca queda en negativo sin explicación y el rojo cuesta.
    /// Por ahora solo el jugador lleva sus cuentas; los demás personajes no.
    /// </summary>
    public sealed class ExpenseSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.gastos";

        /// <summary>Diario: la primera vez que la línea de crédito cubre lo que falta.</summary>
        public const string DeficitJournal = "diario.gastos.linea_credito";

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
            HousingDefinition? housing = HousingRules.RulesFor(_content, player);
            if (player.Lease != null && housing != null)
            {
                // El arriendo sale del contrato que firmó el jugador; con pareja se divide.
                decimal rent = HousingRules.MonthlyRent(_content, player, housing, economy.PriceIndex);
                if (hasPartner)
                {
                    rent = Math.Round(rent / 2, MidpointRounding.AwayFromZero);
                }

                if (rent > 0)
                {
                    lines.Add(new PayslipLine(player.Lease.TypeId, rent));
                    player.Money -= rent;
                }
            }

            if (player.GroceryBasketId != null && player.HasFlag(HousingRules.LivesOnOwnFlag) && GroceryRules.RulesFor(_content, player)?.Basket(player.GroceryBasketId) != null)
            {
                // La comida sale de la compra que eligió el jugador, para todos los de su casa.
                decimal food = PayrollCalculator.ToToday(GroceryRules.MonthlyShare(world, _content, player, player.GroceryBasketId), economy.PriceIndex);
                if (food > 0)
                {
                    lines.Add(new PayslipLine(player.GroceryBasketId, food));
                    player.Money -= food;
                }
            }

            foreach (ServiceDefinition service in ServiceRules.ServicesFor(_content, player))
            {
                // Los servicios que contrató el jugador (el plan del celular...).
                if (!player.Services.TryGetValue(service.Id, out string? optionId) || service.Option(optionId) == null
                    || (service.CompiledConditions != null && !service.CompiledConditions.IsTrue(scope)))
                {
                    continue;
                }

                decimal price = service.Option(optionId)!.Price;
                if (service.Shared && hasPartner)
                {
                    price = Math.Round(price / 2, MidpointRounding.AwayFromZero);
                }

                decimal fee = PayrollCalculator.ToToday(price, economy.PriceIndex);
                if (fee > 0)
                {
                    lines.Add(new PayslipLine(optionId, fee));
                    player.Money -= fee;
                }
            }

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

                decimal today = expense.InTodayMoney
                    ? Math.Round((decimal)Math.Max(0, amount), MidpointRounding.AwayFromZero)
                    : PayrollCalculator.ToToday((decimal)Math.Max(0, amount), economy.PriceIndex);
                if (today > 0 && expense.Income)
                {
                    // Lo que le pagan (la pensión de alimentos que recibe): entra, no es un gasto.
                    player.Money += today;
                }
                else if (today > 0)
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
                CoverDeficit(world, player, rules, economy);
            }
            else
            {
                player.MonthsInRed = 0;
            }
        }

        /// <summary>
        /// Lo que falta pasa a la deuda "deuda_por_deficit" del país (si tiene),
        /// hasta su cupo, y el dinero queda en cero: lo que pasa del cupo son
        /// cuentas que no se pagaron. Esa deuda suma su interés y cobra sus cuotas como
        /// cualquier otra; si no se pagan, lleva al registro de morosos.
        /// </summary>
        private void CoverDeficit(World world, Character player, ExpensesDefinition rules, CountryEconomy economy)
        {
            DebtTypeDefinition? type = rules.DeficitDebt == null ? null : _content.GetCountry(player.Residence!.CountryId).Economy?.Debts?.Type(rules.DeficitDebt);
            if (type == null)
            {
                return;
            }

            Debt? current = player.Debts.FirstOrDefault(d => d.TypeId == type.Id);
            decimal covered = -player.Money;
            if (rules.DeficitLimit.HasValue)
            {
                decimal room = PayrollCalculator.ToToday(rules.DeficitLimit.Value, economy.PriceIndex) - (current?.Balance ?? 0);
                covered = Math.Max(0, Math.Min(covered, room));
            }

            player.Money = 0;
            if (covered <= 0)
            {
                return;
            }

            DebtSystem.Borrow(player, type, covered, economy.PriceIndex);
            player.Money = 0;
            if (current == null)
            {
                world.WriteJournal(player.Id, DeficitJournal);
            }
        }
    }
}

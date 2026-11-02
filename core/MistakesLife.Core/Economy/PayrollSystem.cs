using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas, después de <see cref="EconomySystem"/>: al empezar cada
    /// mes, quien tiene trabajo recibe su sueldo. Si ya tocaba, primero se
    /// reajusta a los precios de hoy; luego se calcula la liquidación con las
    /// reglas de su país y el líquido se suma a su dinero.
    /// </summary>
    public sealed class PayrollSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.sueldos";

        private readonly ContentDatabase _content;

        public PayrollSystem(ContentDatabase content)
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

            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Job != null).OrderBy(c => c.Id))
            {
                if (character.Residence == null)
                {
                    continue;
                }

                EconomyDefinition? rules = _content.GetCountry(character.Residence.CountryId).Economy;
                CountryEconomy? economy = world.EconomyOf(character.Residence.CountryId);
                if (rules == null || economy == null)
                {
                    continue;
                }

                Job job = character.Job!;
                job.AdvanceMonth(economy.PriceIndex, rules.Salaries?.AdjustmentMonths ?? 12);
                Payslip payslip = PayrollCalculator.Compute(context.Date, job.Kind, job.Salary, rules.Payroll, economy.PriceIndex);
                character.Money += payslip.Net;
                character.LastPayslip = payslip;
                character.AddIncome(context.Date.Year, payslip);
            }
        }
    }
}

using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas, justo después de los sueldos: las pensiones, con las
    /// reglas del país ("economia.jubilacion"). Al empezar cada mes:
    /// - el fondo de cada persona gana su rentabilidad;
    /// - quien recibió sueldo con contrato ese mes ahorra en su cuenta la parte de
    ///   su cotización que corresponde (quien trabaja a honorarios ahorra al declarar);
    /// - quien está jubilado recibe su pensión, y quien pidió la pensión del
    ///   Estado la recibe según cuánto es su pensión propia.
    /// Jubilarse lo deciden los eventos (jugador.jubilarse()).
    /// </summary>
    public sealed class PensionSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.pensiones";

        private readonly ContentDatabase _content;

        public PensionSystem(ContentDatabase content)
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
                PensionsDefinition? rules = _content.GetCountry(character.Residence!.CountryId).Economy?.Pensions;
                CountryEconomy? economy = world.EconomyOf(character.Residence.CountryId);
                if (rules == null || economy == null)
                {
                    continue;
                }

                PensionAccountDefinition? account = rules.IndividualAccount;
                if (account != null)
                {
                    if (character.PensionFund > 0)
                    {
                        character.PensionFund *= 1 + (decimal)PensionCalculator.MonthlyReturn(account.RealAnnualReturn);
                    }

                    Payslip? payslip = character.LastPayslip;
                    if (payslip != null && payslip.Date.Equals(context.Date) && payslip.Kind == JobKind.Employee)
                    {
                        decimal saved = payslip.Amount(account.Deduction) * (decimal)account.ShareToAccount;
                        character.PensionFund += saved / (decimal)economy.PriceIndex;
                        character.ContributedMonths++;
                    }
                }

                decimal paid = 0;
                if (character.Pension.HasValue)
                {
                    paid += PayrollCalculator.ToToday(character.Pension.Value, economy.PriceIndex);
                }

                if (character.ReceivesUniversalPension && rules.UniversalPension != null)
                {
                    decimal universal = PensionCalculator.UniversalPension(character.Pension ?? 0, rules.UniversalPension);
                    paid += PayrollCalculator.ToToday(universal, economy.PriceIndex);
                }

                character.Money += paid;
                character.LastPensionPayment = paid;
            }
        }

        /// <summary>
        /// Quien trabaja a honorarios ahorra para su pensión al declarar: la parte
        /// de su base de cotización que el país manda a la cuenta.
        /// </summary>
        public static void SaveFreelance(Character character, PensionsDefinition? rules, AnnualTaxDefinition annualTax, AnnualIncome income, double priceIndex)
        {
            PensionAccountDefinition? account = rules?.IndividualAccount;
            if (account == null || income.FreelanceGross <= 0)
            {
                return;
            }

            decimal saved = income.FreelanceGross * (decimal)annualTax.FreelanceContributionBase * (decimal)account.FreelanceRate;
            character.PensionFund += saved / (decimal)priceIndex;
        }

        /// <summary>
        /// Jubilarse: el fondo se convierte en una pensión fija mensual para el resto
        /// de la vida (0 si no tiene fondo o su país no tiene cuenta individual).
        /// </summary>
        public static void Retire(Character character, PensionsDefinition? rules)
        {
            if (character.Pension.HasValue)
            {
                throw new InvalidOperationException($"{character} ya está jubilado.");
            }

            character.Pension = EstimatedPension(character, rules);
            character.PensionFund = 0;
        }

        /// <summary>La pensión mensual que recibiría si se jubilara hoy, en dinero del inicio del mundo.</summary>
        public static decimal EstimatedPension(Character character, PensionsDefinition? rules)
        {
            PensionAccountDefinition? account = rules?.IndividualAccount;
            return account == null ? 0 : Math.Round(PensionCalculator.MonthlyPension(character.PensionFund, account), MidpointRounding.AwayFromZero);
        }
    }
}

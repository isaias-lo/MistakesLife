using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Reglas de la previsión de salud ("economia.salud" del país): en qué tramo de
    /// la previsión pública queda cada uno según lo que gana, cuánto cuesta un plan
    /// privado a su edad, cambiarse de previsión y cuánto paga por una atención.
    /// Nada se cobra solo: el plan privado lo contrata el jugador, y una atención la
    /// paga cuando decide atenderse. El cobro del plan va en la liquidación
    /// (<see cref="PayrollSystem"/>) o, si no tiene contrato, en los gastos del mes
    /// (<see cref="ExpenseSystem"/>).
    /// </summary>
    public static class HealthCareRules
    {
        /// <summary>El sistema de salud del país donde vive, o null.</summary>
        public static HealthCareDefinition? RulesFor(ContentDatabase content, Character character) =>
            character.Residence == null ? null : content.GetCountry(character.Residence.CountryId).Economy?.HealthCare;

        /// <summary>Su plan privado, si lo tiene y existe en su país; si no, null (está en la pública).</summary>
        public static HealthPlanDefinition? PlanOf(HealthCareDefinition rules, Character character) =>
            character.HealthPlanId == null ? null : rules.Plan(character.HealthPlanId);

        /// <summary>Su previsión: el id del plan privado o el de la pública.</summary>
        public static string Coverage(HealthCareDefinition rules, Character character) =>
            PlanOf(rules, character)?.Id ?? rules.PublicId;

        /// <summary>
        /// Lo que gana al mes para la previsión, en dinero de los paquetes: el sueldo
        /// de un trabajo con cotizaciones (contrato u honorarios) o, si no, su pensión.
        /// Un trabajo informal no cotiza: cuenta como sin ingresos.
        /// </summary>
        public static decimal MonthlyIncome(Character character, double priceIndex)
        {
            double index = priceIndex > 0 ? priceIndex : 1;
            if (character.Job != null && character.Job.Kind != JobKind.Informal)
            {
                return character.Job.Salary / (decimal)index;
            }

            return character.Pension ?? 0;
        }

        /// <summary>
        /// El tramo de la previsión pública que le toca según lo que gana (aunque tenga un
        /// plan privado: así se sabe en cuál quedaría si vuelve).
        /// </summary>
        public static string Tier(HealthCareDefinition rules, Character character, decimal minimumWage, double priceIndex)
        {
            decimal income = MonthlyIncome(character, priceIndex);
            if (income <= 0 || rules.Tiers.Count == 0)
            {
                return rules.NoIncomeTierId;
            }

            foreach (HealthTierDefinition tier in rules.Tiers)
            {
                if (!tier.UpToMinimumWages.HasValue || income <= minimumWage * (decimal)tier.UpToMinimumWages.Value)
                {
                    return tier.Id;
                }
            }

            return rules.Tiers[rules.Tiers.Count - 1].Id;
        }

        /// <summary>Lo que cuesta al mes ese plan a su edad (precio base por el factor), en dinero de los paquetes.</summary>
        public static decimal PlanPrice(HealthCareDefinition rules, HealthPlanDefinition plan, int age) =>
            Math.Round(plan.BasePrice * (decimal)rules.AgeFactor(age), MidpointRounding.AwayFromZero);

        /// <summary>
        /// Se cambia a esa previsión: un plan privado de su país o la pública
        /// ("prevision.chile.fonasa"). Es un error si no existe en su país.
        /// </summary>
        public static void Join(ContentDatabase content, Character character, string coverageId)
        {
            HealthCareDefinition rules = RulesFor(content, character)
                ?? throw new ScriptException($"jugador.afiliarse('{coverageId}'): su país no tiene sistema de salud (economia.salud).");
            if (coverageId == rules.PublicId)
            {
                character.HealthPlanId = null;
                return;
            }

            if (rules.Plan(coverageId) == null)
            {
                throw new ScriptException($"jugador.afiliarse('{coverageId}'): su país no tiene esa previsión en economia.salud.");
            }

            character.HealthPlanId = coverageId;
        }

        /// <summary>Si puede atenderse ahí con su previsión (el consultorio es solo de la pública).</summary>
        public static bool CanUse(HealthCareDefinition rules, Character character, HealthCareServiceDefinition service) =>
            !service.PublicOnly || PlanOf(rules, character) == null;

        /// <summary>
        /// Lo que paga por esa atención, en dinero de los paquetes: con un plan
        /// privado, el precio menos lo que cubre el plan; en la pública con ingresos, el
        /// bono (o el precio si no hay bono); sin ingresos no puede comprar bonos y paga
        /// el precio (salvo en la red pública, que es gratis).
        /// </summary>
        public static decimal Copay(HealthCareDefinition rules, Character character, HealthCareServiceDefinition service, decimal minimumWage, double priceIndex)
        {
            HealthPlanDefinition? plan = PlanOf(rules, character);
            if (plan != null)
            {
                return Math.Round(service.Price * (decimal)(1 - plan.Coverage), MidpointRounding.AwayFromZero);
            }

            bool canBuyVoucher = Tier(rules, character, minimumWage, priceIndex) != rules.NoIncomeTierId;
            return canBuyVoucher && service.PublicCopay.HasValue ? Math.Min(service.PublicCopay.Value, service.Price) : service.Price;
        }

        /// <summary>
        /// Se atiende: paga el copago a precios de hoy, que queda como una línea más de
        /// la cartola del mes, y se anota la fecha. Es un error si su previsión no le deja atenderse ahí.
        /// </summary>
        public static void Attend(World world, ContentDatabase content, Character character, string serviceId)
        {
            HealthCareDefinition rules = RulesFor(content, character)
                ?? throw new ScriptException($"jugador.atenderse('{serviceId}'): su país no tiene sistema de salud (economia.salud).");
            HealthCareServiceDefinition service = rules.Service(serviceId)
                ?? throw new ScriptException($"jugador.atenderse('{serviceId}'): su país no tiene esa atención en economia.salud.atenciones.");
            if (!CanUse(rules, character, service))
            {
                throw new ScriptException($"jugador.atenderse('{serviceId}'): esa atención es solo de la previsión pública (revisa jugador.puede_atenderse).");
            }

            double index = world.EconomyOf(character.Residence!.CountryId)?.PriceIndex ?? 1;
            decimal minimum = content.GetCountry(character.Residence.CountryId).Economy?.Salaries?.Minimum ?? 0;
            decimal paid = PayrollCalculator.ToToday(Copay(rules, character, service, minimum, index), index);
            character.LastHealthCareDate = world.Date;
            if (paid > 0)
            {
                // Queda en la cartola del mes, para que se vea en qué se fue la plata.
                character.Money -= paid;
                character.LastExpenses = character.LastExpenses.Concat(new[] { new PayslipLine(service.Id, paid) }).ToList();
            }
        }

        /// <summary>
        /// Lo que el plan privado cuesta más que la cotización que ya paga, a precios de
        /// hoy, para quien no tiene contrato (en la liquidación lo descuenta el
        /// empleador): sin trabajo o informal paga todo el plan; a honorarios, el plan
        /// menos la parte de la retención que va a salud.
        /// </summary>
        public static decimal MonthlyPlanCharge(HealthCareDefinition rules, Character character, GameDate date, double priceIndex)
        {
            HealthPlanDefinition? plan = PlanOf(rules, character);
            if (plan == null || (character.Job != null && character.Job.Kind == JobKind.Employee))
            {
                return 0;
            }

            decimal price = PayrollCalculator.ToToday(PlanPrice(rules, plan, character.AgeInYears(date)), priceIndex);
            if (character.Job != null && character.Job.Kind == JobKind.Freelance)
            {
                price -= Math.Round(character.Job.Salary * (decimal)rules.FreelanceRate, MidpointRounding.AwayFromZero);
            }

            return Math.Max(0, price);
        }
    }
}

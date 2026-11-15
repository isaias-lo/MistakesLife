using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Las vacaciones legales (ver <see cref="VacationsDefinition"/>): quien trabaja con
    /// contrato gana sus días mes a mes; los toma cuando quiere (un momento que pide el
    /// jugador) y, si se va sin tomarlas, se le pagan.
    /// </summary>
    public static class VacationRules
    {
        /// <summary>Días al año que le tocan hoy en ese trabajo (con el feriado progresivo, si hay).</summary>
        public static int DaysPerYear(VacationsDefinition rules, Character character, Job job) =>
            DaysPerYear(rules, job.MonthsInJob, character.ContributedMonths);

        /// <summary>Días al año con tantos meses con el empleador de ahora y tantos meses cotizados en toda la vida.</summary>
        public static int DaysPerYear(VacationsDefinition rules, int monthsWithEmployer, int contributedMonths)
        {
            int yearsHere = monthsWithEmployer / 12;
            VacationTierDefinition? tier = rules.Tiers
                .Where(t => t.FromYears <= yearsHere)
                .OrderByDescending(t => t.FromYears)
                .FirstOrDefault();
            int days = tier?.Days ?? 0;
            if (rules.ProgressiveFromYears > 0 && rules.ProgressiveEveryYears > 0)
            {
                // Feriado progresivo: los años de antes (con otros empleadores) cuentan hasta un
                // tope; pasado el mínimo, un día más por cada tantos años nuevos.
                int priorYears = Math.Max(0, contributedMonths - monthsWithEmployer) / 12;
                int totalYears = Math.Min(priorYears, rules.ProgressiveMaxPriorYears) + yearsHere;
                if (totalYears >= rules.ProgressiveFromYears)
                {
                    days += (totalYears - rules.ProgressiveFromYears) / rules.ProgressiveEveryYears;
                }
            }

            return days;
        }

        /// <summary>Pasa un mes trabajando con contrato: gana la parte del mes, hasta el tope que se puede juntar.</summary>
        public static void Accrue(VacationsDefinition rules, Character character, Job job)
        {
            if (job.Kind != JobKind.Employee)
            {
                job.VacationDays = 0;
                return;
            }

            int perYear = DaysPerYear(rules, character, job);
            double max = perYear * Math.Max(1, rules.MaxYearsSaved);
            job.VacationDays = Math.Min(max, job.VacationDays + perYear / 12.0);
        }

        /// <summary>Cuánto se le paga por los días que no tomó, en la moneda de hoy.</summary>
        public static decimal PayFor(VacationsDefinition? rules, Job job)
        {
            if (rules == null || !rules.PaidOnLeaving || job.VacationDays <= 0 || rules.DayDivisor <= 0)
            {
                return 0;
            }

            decimal perDay = job.Salary / (decimal)rules.DayDivisor;
            return Math.Round(perDay * (decimal)(job.VacationDays * rules.PaidDaysPerDay), MidpointRounding.AwayFromZero);
        }
    }
}

using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Reglas de la búsqueda de trabajo. Nada llega solo: el trabajo es la
    /// consecuencia de buscarlo. Cada semana, con la forma de buscar que eligió, hay
    /// una probabilidad de que lo llamen a una entrevista (más con experiencia o
    /// título; menos con edad avanzada, una cesantía larga o una economía mala). En
    /// la entrevista cuentan cómo se preparó y su personalidad; si queda, recibe una
    /// oferta. Los números son del país (economia.busqueda_trabajo).
    /// </summary>
    public static class JobSearchRules
    {
        /// <summary>Marca de quien está buscando trabajo (la usan los eventos y el simulador).</summary>
        public const string SearchingFlag = "buscando_trabajo";

        /// <summary>Marca de quien trabaja.</summary>
        public const string WorkingFlag = "trabajando";

        public const string RejectionJournal = "diario.busqueda.rechazo";
        public const string WithdrawnJournal = "diario.busqueda.oferta_retirada";
        public const string HiredJournal = "diario.busqueda.nuevo_trabajo";

        /// <summary>Edades en que un conocido puede recomendar a alguien en su trabajo.</summary>
        public const int ContactMinAge = 18;
        public const int ContactMaxAge = 70;

        /// <summary>Probabilidad mínima y máxima de quedar después de una entrevista.</summary>
        public const double MinSuccess = 0.05;
        public const double MaxSuccess = 0.95;

        /// <summary>Cómo se busca trabajo en el país donde vive, o null.</summary>
        public static JobSearchDefinition? Definition(ContentDatabase content, Character character) =>
            character.Residence == null ? null : content.GetCountry(character.Residence.CountryId).Economy?.JobSearch;

        /// <summary>
        /// El conocido que lo puede recomendar: una amistad o un colega vivo, en edad de
        /// trabajar (<see cref="ContactMinAge"/> a <see cref="ContactMaxAge"/> años), que le
        /// tenga al menos ese cariño. Si hay varios, el que más lo quiere.
        /// </summary>
        public static Character? FindContact(World world, Character character, int minAffection)
        {
            Character? best = null;
            int bestAffection = -1;
            foreach (Relationship mine in character.Relationships.OrderBy(r => r.OtherId))
            {
                if (mine.Bond != RelationshipBond.Friend && mine.Bond != RelationshipBond.Colleague)
                {
                    continue;
                }

                if (!world.TryGetCharacter(mine.OtherId, out Character? other) || !other.IsAlive)
                {
                    continue;
                }

                int age = other.AgeInYears(world.Date);
                if (age < ContactMinAge || age > ContactMaxAge)
                {
                    continue;
                }

                int affection = RelationshipRules.Feelings(world, other, character).Affection;
                if (affection >= minAffection && affection > bestAffection)
                {
                    best = other;
                    bestAffection = affection;
                }
            }

            return best;
        }

        /// <summary>Probabilidad de que lo llamen a una entrevista esta semana (0 a 1).</summary>
        public static double InterviewChance(World world, ContentDatabase content, Character character, JobSearch search)
        {
            JobSearchDefinition? rules = Definition(content, character);
            JobSearchMethodDefinition? method = search.MethodId == null ? null : rules?.Method(search.MethodId);
            if (rules == null || method == null)
            {
                return 0;
            }

            double chance = method.WeeklyInterview;
            double experience = 1 + (character.ContributedMonths / 12.0) * rules.PerYearOfExperience;
            chance *= Math.Min(Math.Max(1, rules.ExperienceCap), experience);
            if (character.Degrees.Count > 0)
            {
                chance *= rules.DegreeFactor;
            }

            if (rules.HardAge.HasValue && character.AgeInYears(world.Date) >= rules.HardAge.Value)
            {
                chance *= rules.AgeFactor;
            }

            if (rules.LongSearchWeeks.HasValue && search.Weeks >= rules.LongSearchWeeks.Value)
            {
                chance *= rules.LongSearchFactor;
            }

            if (method.NeedsContact && !(search.ContactId.HasValue && world.TryGetCharacter(search.ContactId.Value, out Character? contact) && contact.IsAlive))
            {
                // El contacto murió: queda como mandar el currículum sin recomendación.
                chance /= 2;
            }

            CountryDefinition country = content.GetCountry(character.Residence!.CountryId);
            string? phaseId = world.EconomyOf(country.Id)?.PhaseId;
            chance *= (phaseId == null ? null : country.Economy?.Cycle?.Phase(phaseId))?.HiringFactor ?? 1;
            return Math.Max(0, Math.Min(1, chance));
        }

        /// <summary>
        /// Probabilidad de quedar en la entrevista: la base del país, lo que suma la forma
        /// de buscar, los puntos de cómo fue (de la opción que eligió, 10 = 10 % más) y su
        /// extraversión (una persona extrovertida cae mejor en una conversación corta).
        /// </summary>
        public static double SuccessChance(JobSearchDefinition rules, JobSearchMethodDefinition? method, Character character, double points)
        {
            double chance = rules.InterviewSuccess + (method?.ExtraSuccess ?? 0) + points / 100.0
                + (character.Personality.Extraversion - 50) / 500.0;
            return Math.Max(MinSuccess, Math.Min(MaxSuccess, chance));
        }

        /// <summary>
        /// Va a la entrevista: queda (pasa a tener una oferta) o no (sigue buscando y lo
        /// anota en su diario). Devuelve si quedó.
        /// </summary>
        public static bool Interview(World world, ContentDatabase content, Character character, double points)
        {
            JobSearch search = character.JobSearch ?? throw new InvalidOperationException($"{character} no está buscando trabajo.");
            if (search.Stage != JobSearchStage.Interview)
            {
                throw new InvalidOperationException($"{character} no tiene una entrevista pendiente.");
            }

            JobSearchDefinition rules = Definition(content, character) ?? new JobSearchDefinition();
            JobSearchMethodDefinition? method = search.MethodId == null ? null : rules.Method(search.MethodId);
            GameRandom random = world.Random.Fork("busqueda.entrevista." + character.Id + "." + world.Date.TotalWeeks);
            if (random.Chance(SuccessChance(rules, method, character, points)))
            {
                search.Stage = JobSearchStage.Offer;
                search.Negotiated = false;
                search.Raise = 0;
                return true;
            }

            search.Stage = JobSearchStage.Searching;
            search.Rejections++;
            world.WriteJournal(character.Id, RejectionJournal);
            return false;
        }

        /// <summary>
        /// Pide más sueldo por la oferta: pueden subirlo, mantenerlo o retirar la oferta
        /// (entonces sigue buscando y lo anota en su diario). Devuelve si la oferta sigue.
        /// </summary>
        public static bool Negotiate(World world, ContentDatabase content, Character character)
        {
            JobSearch search = RequireOffer(character);
            JobNegotiationDefinition rules = (Definition(content, character) ?? new JobSearchDefinition()).Negotiation;
            GameRandom random = world.Random.Fork("busqueda.negociar." + character.Id + "." + world.Date.TotalWeeks);
            double roll = random.NextDouble();
            search.Negotiated = true;
            if (roll < rules.RaiseChance)
            {
                search.Raise = rules.Raise;
                return true;
            }

            if (roll < rules.RaiseChance + rules.WithdrawChance)
            {
                search.Stage = JobSearchStage.Searching;
                search.Negotiated = false;
                world.WriteJournal(character.Id, WithdrawnJournal);
                return false;
            }

            search.Raise = 0;
            return true;
        }

        /// <summary>Rechaza la oferta y sigue buscando.</summary>
        public static void Reject(Character character)
        {
            JobSearch search = RequireOffer(character);
            search.Stage = JobSearchStage.Searching;
            search.Negotiated = false;
            search.Raise = 0;
        }

        /// <summary>
        /// Sueldo de una oferta fuera de su carrera, en dinero del inicio del mundo: quien no
        /// tiene título ni experiencia (pocos meses cotizados) parte con el mínimo; el resto,
        /// con lo que paga el mercado por su título (o lo típico sin título).
        /// </summary>
        public static decimal GeneralSalary(JobSearchDefinition rules, SalaryDefinition salaries, Character character, double degreeSalary)
        {
            if (character.Degrees.Count == 0 && character.ContributedMonths < rules.MonthsForTypicalSalary)
            {
                return salaries.Minimum;
            }

            return (decimal)degreeSalary;
        }

        internal static JobSearch RequireOffer(Character character)
        {
            JobSearch? search = character.JobSearch;
            if (search == null || search.Stage != JobSearchStage.Offer)
            {
                throw new InvalidOperationException($"{character} no tiene una oferta de trabajo.");
            }

            return search;
        }
    }
}

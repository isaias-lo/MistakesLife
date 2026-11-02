using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de personajes: los descendientes del linaje que nadie juega (hijos,
    /// nietos, bisnietos de los jugadores) también forman pareja, tienen hijos y
    /// se separan, con las probabilidades del campo "familia_no_jugada" de su país.
    /// Así, cuando el jugador sigue con un hijo adulto, ese hijo llega con su
    /// propia familia y el linaje puede continuar.
    ///
    /// Cuando a un hijo del jugador le nace un hijo, el jugador escribe en su
    /// diario "diario.nace_nieta" o "diario.nace_nieto".
    /// </summary>
    public sealed class RelativesFamilySystem : IWeeklySystem
    {
        public const string SystemId = "sistema.familia_no_jugada";

        private readonly ContentDatabase _content;

        public RelativesFamilySystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Characters;

        public void Tick(World world, WeekContext context)
        {
            foreach (Character person in NonPlayedDescendants(world))
            {
                FamilyRatesDefinition? rates = person.Residence == null ? null : _content.GetCountry(person.Residence.CountryId).NonPlayedFamily;
                if (rates == null || !person.IsAlive)
                {
                    continue;
                }

                var random = context.RandomFor(SystemId + "." + person.Id);
                int age = person.AgeInYears(context.Date);
                Character? partner = person.PartnerId.HasValue ? world.GetCharacter(person.PartnerId.Value) : null;
                if (partner == null)
                {
                    if (age >= rates.PartnerMinAge && random.Chance(Weekly(rates.PartnerPerYear)))
                    {
                        FamilyFactory.CreatePartner(world, _content, person);
                    }

                    continue;
                }

                if (random.Chance(Weekly(rates.SeparationPerYear)))
                {
                    FamilyFactory.EndPartnership(world, person);
                    continue;
                }

                Character? woman = person.Sex == Sex.Female ? person : partner.Sex == Sex.Female ? partner : null;
                // Hijos biológicos: hace falta una mujer y un hombre (por sexo al nacer).
                bool fertile = woman != null && person.Sex != partner.Sex && woman.AgeInYears(context.Date) <= rates.FertileMaxAge;
                int livingChildren = world.ChildrenOf(person.Id).Count(c => c.IsAlive);
                bool sameSexCouple = person.Sex == partner.Sex;
                bool adopts = sameSexCouple && age <= FamilyFactory.MaxAdoptionAge && livingChildren < rates.MaxChildren && random.Chance(Weekly(rates.AdoptionPerYear));
                if (adopts || (fertile && livingChildren < rates.MaxChildren && random.Chance(Weekly(rates.ChildPerYear))))
                {
                    Character child = adopts ? FamilyFactory.Adopt(world, _content, person) : FamilyFactory.CreateChild(world, _content, person);
                    if (person.IsChildOf(world.PlayerId))
                    {
                        bool girl = child.Sex == Sex.Female;
                        string key = child.Adopted
                            ? (girl ? "diario.llega_nieta" : "diario.llega_nieto")
                            : (girl ? "diario.nace_nieta" : "diario.nace_nieto");
                        world.WriteJournal(world.PlayerId, key);
                    }
                }
            }
        }

        /// <summary>
        /// Los descendientes vivos de los jugadores (actual y anteriores) que no son
        /// el jugador, ordenados por id. Las parejas que llegan de fuera no cuentan:
        /// su familia propia no se simula.
        /// </summary>
        public static IReadOnlyList<Character> NonPlayedDescendants(World world)
        {
            var lineage = new HashSet<int>(world.FormerPlayers) { world.PlayerId };
            var found = new SortedDictionary<int, Character>();
            var pending = new Queue<int>(lineage);
            while (pending.Count > 0)
            {
                foreach (Character child in world.ChildrenOf(pending.Dequeue()))
                {
                    if (!lineage.Contains(child.Id) && !found.ContainsKey(child.Id))
                    {
                        found.Add(child.Id, child);
                        pending.Enqueue(child.Id);
                    }
                }
            }

            return found.Values.Where(c => c.IsAlive).ToList();
        }

        private static double Weekly(double perYear) => perYear >= 1 ? 1 : 1 - Math.Pow(1 - perYear, 1.0 / GameDate.WeeksPerYear);
    }
}

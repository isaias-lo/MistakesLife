using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// La vida de las empresas: una vez al año cada empresa donde trabaja alguien puede
    /// quebrar, crecer o achicarse, con las probabilidades de su tamaño en su país. Las
    /// quiebras se multiplican en las fases malas del ciclo (campo "quiebras").
    /// Si quiebra la empresa del jugador, se detiene el tiempo y un evento del país
    /// cuenta qué pasa (lee jugador.empresa_quebro); los demás pierden el trabajo de una vez.
    /// </summary>
    public sealed class CompanySystem : IWeeklySystem
    {
        public const string SystemId = "sistema.empresas";

        public const string BankruptcyJournal = "diario.empresa.quiebra";
        public const string GrowthJournal = "diario.empresa.crece";
        public const string ShrinkJournal = "diario.empresa.achica";

        /// <summary>Motivo para detener el tiempo cuando quiebra la empresa del jugador.</summary>
        public const string StopReasonBankruptcy = "empresa.quiebra";

        private readonly ContentDatabase _content;

        public CompanySystem(ContentDatabase content)
        {
            _content = content;
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            if (context.Date.WeekOfYear != 1)
            {
                return;
            }

            var workers = world.Characters
                .Where(c => c.IsAlive && c.Job?.CompanyId != null)
                .OrderBy(c => c.Id)
                .GroupBy(c => c.Job!.CompanyId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (Company company in world.Companies.Where(c => workers.ContainsKey(c.Id)).ToList())
            {
                if (!company.IsOpen)
                {
                    // Quien siguió en una empresa quebrada (si el país no tiene el evento) la deja ahora.
                    workers[company.Id].ForEach(w => w.Job = null);
                    continue;
                }

                CountryDefinition country = _content.GetCountry(company.CountryId);
                CompaniesDefinition? companies = country.Economy?.Companies;
                CompanySizeDefinition? size = companies?.Size(company.SizeId);
                if (size == null)
                {
                    continue;
                }

                GameRandom random = world.Random.Fork("empresa.ano." + company.Id + "." + context.Date.Year);
                string? phaseId = world.EconomyOf(company.CountryId)?.PhaseId;
                double factor = (phaseId == null ? null : country.Economy?.Cycle?.Phase(phaseId))?.BankruptcyFactor ?? 1;
                if (random.Chance(System.Math.Min(1, size.AnnualBankruptcy * factor)))
                {
                    Close(world, context, company, workers[company.Id]);
                }
                else if (random.Chance(size.AnnualGrowth) && companies!.Bigger(size.Id) is CompanySizeDefinition bigger)
                {
                    company.SizeId = bigger.Id;
                    Tell(world, workers[company.Id], GrowthJournal);
                }
                else if (random.Chance(size.AnnualShrink) && companies!.Smaller(size.Id) is CompanySizeDefinition smaller)
                {
                    company.SizeId = smaller.Id;
                    Tell(world, workers[company.Id], ShrinkJournal);
                }
            }
        }

        private static void Close(World world, WeekContext context, Company company, System.Collections.Generic.List<Character> people)
        {
            company.ClosedDate = context.Date;
            foreach (Character worker in people)
            {
                if (worker.Id == world.PlayerId)
                {
                    world.WriteJournal(worker.Id, BankruptcyJournal);
                    context.RequestStop(StopReasonBankruptcy);
                }
                else
                {
                    worker.Job = null;
                }
            }
        }

        private static void Tell(World world, System.Collections.Generic.IEnumerable<Character> people, string key)
        {
            if (people.Any(p => p.Id == world.PlayerId))
            {
                world.WriteJournal(world.PlayerId, key);
            }
        }
    }
}

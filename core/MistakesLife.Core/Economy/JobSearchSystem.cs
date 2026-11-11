using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas: la búsqueda de trabajo avanza una semana. Quien eligió cómo
    /// buscar manda sus postulaciones y puede recibir una llamada a entrevista
    /// (<see cref="JobSearchRules.InterviewChance"/>). La entrevista y la oferta las
    /// cuentan los eventos (jugador.entrevista_pendiente, jugador.tiene_oferta).
    /// Si deja de buscar (pierde la marca "buscando_trabajo"), la búsqueda se borra.
    /// </summary>
    public sealed class JobSearchSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.busqueda_trabajo";

        private readonly ContentDatabase _content;

        public JobSearchSystem(ContentDatabase content)
        {
            _content = content;
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            foreach (Character character in world.Characters.Where(c => c.JobSearch != null).ToList())
            {
                JobSearch search = character.JobSearch!;
                if (!character.IsAlive || !character.HasFlag(JobSearchRules.SearchingFlag))
                {
                    character.JobSearch = null;
                    continue;
                }

                if (search.MethodId == null || search.Stage != JobSearchStage.Searching)
                {
                    continue;
                }

                JobSearchMethodDefinition? method = JobSearchRules.Definition(_content, character)?.Method(search.MethodId);
                if (method == null)
                {
                    // Se mudó a un país sin esa forma de buscar: vuelve a elegir.
                    search.MethodId = null;
                    continue;
                }

                search.Weeks++;
                search.Applications += method.ApplicationsPerWeek;
                double chance = JobSearchRules.InterviewChance(world, _content, character, search);
                if (world.Random.Fork("busqueda.semana." + character.Id + "." + context.Date.TotalWeeks).Chance(chance))
                {
                    search.Stage = JobSearchStage.Interview;
                    search.Interviews++;
                    world.WriteJournal(character.Id, JobSearchRules.InterviewJournal);
                }
            }
        }
    }
}

using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Fase de sistemas, después de los sueldos: al empezar cada mes, quien trabaja con
    /// contrato gana la parte del mes de sus vacaciones legales (ver <see cref="VacationRules"/>).
    /// </summary>
    public sealed class VacationSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.vacaciones";

        private readonly ContentDatabase _content;

        public VacationSystem(ContentDatabase content)
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

            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Job != null && c.Residence != null).OrderBy(c => c.Id))
            {
                VacationsDefinition? rules = _content.GetCountry(character.Residence!.CountryId).Economy?.Vacations;
                if (rules != null)
                {
                    VacationRules.Accrue(rules, character, character.Job!);
                }
            }
        }
    }
}

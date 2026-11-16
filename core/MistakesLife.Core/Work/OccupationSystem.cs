using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Fase de sistemas, justo antes de los sueldos: al empezar cada mes, quien trabaja en un
    /// oficio cierra la meta del mes que terminó (ver <see cref="OccupationRules"/>), para que
    /// la comisión o el trato entren en la liquidación de este mes.
    /// </summary>
    public sealed class OccupationSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.oficios";

        private readonly ContentDatabase _content;

        public OccupationSystem(ContentDatabase content)
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
                OccupationRules.CloseMonth(world, _content, character, context.Date);
            }
        }
    }
}

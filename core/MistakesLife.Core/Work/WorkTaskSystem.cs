using System;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Fase de sistemas, después del equipo de la pega: al empezar cada mes le tocan al jugador
    /// las tareas de su cargo (ver <see cref="WorkTaskRules"/>). Va antes de los eventos, para que
    /// la tarea de esta semana ya sea un momento.
    /// </summary>
    public sealed class WorkTaskSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.tareas_pega";

        private readonly ContentDatabase _content;

        public WorkTaskSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            if (!context.Date.IsFirstWeekOfMonth || !world.Player.IsAlive)
            {
                return;
            }

            WorkTaskRules.StartMonth(world, _content, world.Player, context.Date);
        }
    }
}

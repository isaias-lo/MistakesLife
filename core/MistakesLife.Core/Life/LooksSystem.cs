using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de sistemas: la apariencia del jugador. La primera vez se calcula lo que heredó
    /// de su mamá y su papá; después, cada mes, se acerca un punto a la que deja su vida
    /// (<see cref="LooksRules.Target"/>). Usa su propio azar, así no cambia el de los demás sistemas.
    /// </summary>
    public sealed class LooksSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.apariencia";

        private readonly ContentDatabase _content;

        public LooksSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive)
            {
                return;
            }

            if (!player.InheritedLooks.HasValue)
            {
                player.InheritedLooks = LooksRules.Inherit(world, player, context.RandomFor(SystemId).Fork("persona." + player.Id));
                player.Stats.Looks = player.InheritedLooks.Value;
                return;
            }

            if (!context.Date.IsFirstWeekOfMonth)
            {
                return;
            }

            int target = LooksRules.Target(world, _content, player);
            player.Stats.Looks += Math.Sign(target - player.Stats.Looks);
        }
    }
}

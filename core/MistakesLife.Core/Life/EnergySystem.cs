using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de sistemas: la energía del jugador se acerca cada semana a la que deja su vida
    /// (<see cref="EnergyRules.Target"/>). Cansarse es rápido y recuperarse cuesta más con los años.
    /// </summary>
    public sealed class EnergySystem : IWeeklySystem
    {
        public const string SystemId = "sistema.energia";

        private readonly ContentDatabase _content;

        public EnergySystem(ContentDatabase content)
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

            int target = EnergyRules.Target(world, _content, player);
            int energy = player.Stats.Energy;
            if (target < energy)
            {
                player.Stats.Energy = Math.Max(target, energy - EnergyRules.DrainPerWeek);
            }
            else if (target > energy)
            {
                int step = EnergyRules.RecoveryPerWeek(player.AgeInYears(context.Date), context.Date.WeekOfYear);
                player.Stats.Energy = Math.Min(target, energy + step);
            }
        }
    }
}

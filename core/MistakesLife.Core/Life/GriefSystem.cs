using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de personajes, justo después de la mortalidad: el duelo del jugador se nota cada
    /// semana. Mientras tiene pena (ver <see cref="GriefRules"/>), su felicidad y su energía no
    /// pueden pasar de un techo, que sube a medida que la pena baja. Los duelos viejos se olvidan
    /// pasado el primer año, para que la partida no crezca sin fin.
    /// </summary>
    public sealed class GriefSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.duelo";

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Characters;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.Griefs.Count == 0)
            {
                return;
            }

            player.Griefs.RemoveAll(g => context.Date.WeeksSince(g.Since) > GriefRules.RememberWeeks
                && GriefRules.Pain(g, context.Date) == 0);
            int pain = GriefRules.CurrentPain(player, context.Date);
            if (pain <= 0)
            {
                return;
            }

            int happinessCap = 100 - (int)(pain * GriefRules.HappinessPerPain);
            int energyCap = 100 - (int)(pain * GriefRules.EnergyPerPain);
            if (player.Stats.Happiness > happinessCap)
            {
                player.Stats.Happiness = happinessCap;
            }

            if (player.Stats.Energy > energyCap)
            {
                player.Stats.Energy = energyCap;
            }
        }
    }
}

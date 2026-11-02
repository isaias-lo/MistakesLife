using MistakesLife.Core.Characters;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Simulation
{
    /// <summary>
    /// Sistema de ejemplo: anota los cumpleaños del jugador en el diario.
    /// Sirve para comprobar que el ciclo semanal funciona de punta a punta.
    /// </summary>
    public sealed class BirthdaySystem : IWeeklySystem
    {
        public string Id => "sistema.cumpleanos";

        public WeekPhase Phase => WeekPhase.Consequences;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            // La edad al terminar esta semana.
            int ageInWeeks = player.AgeInWeeks(context.Date.AddWeeks(1));
            if (ageInWeeks > 0 && ageInWeeks % GameDate.WeeksPerYear == 0)
            {
                world.WriteJournal(player.Id, "diario.cumpleanos");
            }
        }
    }
}

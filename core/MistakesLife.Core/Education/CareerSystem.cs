using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Avanza los estudios superiores: cada 26 semanas de estudio se aprueba un semestre.
    /// Cuándo alguien se titula, reprueba o deserta lo deciden los eventos de cada carrera.
    /// También avanza a quienes trabajan en su carrera: cada mes suman experiencia en su
    /// cargo y su desempeño se mueve (ver CareerLadder). Los ascensos los dan los eventos.
    /// </summary>
    public sealed class CareerSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.carreras";

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Studies != null))
            {
                character.Studies!.AdvanceWeek();
            }

            if (!context.Date.IsFirstWeekOfMonth)
            {
                return;
            }

            foreach (Character worker in world.Characters.Where(c => c.IsAlive && c.Job?.Position != null).OrderBy(c => c.Id))
            {
                Economy.CareerLadder.AdvanceMonth(worker);
            }
        }
    }
}

using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Simulation
{
    /// <summary>
    /// Un módulo de la simulación (salud, economía, estudios…).
    /// Cada sistema declara en qué fase participa y hace su trabajo una vez por semana.
    /// Agregar un sistema nuevo no obliga a tocar los demás.
    /// </summary>
    public interface IWeeklySystem
    {
        /// <summary>Id estable del sistema, por ejemplo "sistema.salud".</summary>
        string Id { get; }

        /// <summary>Fase de la semana en que corre.</summary>
        WeekPhase Phase { get; }

        /// <summary>Hace el trabajo de una semana.</summary>
        void Tick(World world, WeekContext context);
    }
}

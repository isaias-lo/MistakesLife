using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Simulation
{
    /// <summary>
    /// Datos de la semana que se está simulando, compartidos por todos los sistemas.
    /// </summary>
    public sealed class WeekContext
    {
        private readonly GameRandom _weekRandom;

        internal WeekContext(GameDate date, GameRandom weekRandom)
        {
            Date = date;
            _weekRandom = weekRandom;
        }

        /// <summary>Fecha de la semana que se simula.</summary>
        public GameDate Date { get; }

        /// <summary>
        /// Si algún sistema la activa, el tiempo se detiene al terminar esta semana
        /// (un "día clave" que el jugador debe vivir con calma).
        /// </summary>
        public bool StopRequested { get; private set; }

        /// <summary>Motivo de la detención (clave de texto), si la hay.</summary>
        public string? StopReason { get; private set; }

        public void RequestStop(string reasonKey)
        {
            if (StopRequested)
            {
                return;
            }

            StopRequested = true;
            StopReason = reasonKey;
        }

        /// <summary>
        /// Azar propio de un sistema para esta semana. Cada sistema recibe su
        /// propio flujo, así que lo que haga uno no altera el azar de otro.
        /// </summary>
        public GameRandom RandomFor(string systemId) => _weekRandom.Fork(systemId);
    }
}

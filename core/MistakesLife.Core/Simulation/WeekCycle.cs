using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Simulation
{
    /// <summary>
    /// El motor que avanza el mundo de a una semana, corriendo los sistemas
    /// fase por fase y, dentro de cada fase, en el orden en que se registraron.
    /// </summary>
    public sealed class WeekCycle
    {
        private readonly List<IWeeklySystem> _systems = new List<IWeeklySystem>();

        public IReadOnlyList<IWeeklySystem> Systems => _systems;

        public WeekCycle Register(IWeeklySystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (_systems.Any(s => s.Id == system.Id))
            {
                throw new InvalidOperationException($"El sistema {system.Id} ya está registrado.");
            }

            _systems.Add(system);
            return this;
        }

        /// <summary>Simula una semana completa y avanza la fecha.</summary>
        public WeekContext AdvanceWeek(World world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            // Un flujo de azar por semana, derivado de la semilla y la fecha:
            // la semana N siempre recibe el mismo azar en la misma partida.
            var weekRandom = world.Random.Fork("semana." + world.Date.TotalWeeks);
            var context = new WeekContext(world.Date, weekRandom);

            foreach (WeekPhase phase in Enum.GetValues(typeof(WeekPhase)).Cast<WeekPhase>().OrderBy(p => (int)p))
            {
                foreach (IWeeklySystem system in _systems)
                {
                    if (system.Phase == phase)
                    {
                        system.Tick(world, context);
                    }
                }
            }

            world.AdvanceWeek();
            return context;
        }

        /// <summary>
        /// Avanza hasta <paramref name="maxWeeks"/> semanas, o menos si una semana
        /// pide detener el tiempo o el jugador muere. Devuelve las semanas simuladas.
        /// </summary>
        public int AdvanceWeeks(World world, int maxWeeks)
        {
            int simulated = 0;
            while (simulated < maxWeeks && world.Player.IsAlive)
            {
                WeekContext context = AdvanceWeek(world);
                simulated++;
                if (context.StopRequested)
                {
                    break;
                }
            }

            return simulated;
        }
    }
}

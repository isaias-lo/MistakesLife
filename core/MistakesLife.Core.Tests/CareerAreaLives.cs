using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Vidas que empiezan a trabajar en cada área de cada carrera a los 25 años y siguen
    /// hasta los 60, decidiendo solas. Sirven para revisar que los eventos de áreas poco
    /// comunes (por ejemplo, peritajes forenses) se puedan vivir: en vidas al azar casi
    /// nadie llega a ellas.
    /// </summary>
    internal static class CareerAreaLives
    {
        /// <summary>Marcas que piden las áreas especializadas de los paquetes.</summary>
        private static readonly string[] RequirementFlags =
        {
            "certificacion_ciberseguridad", "certificacion_datos", "diplomado_tributacion", "curso_legislacion_laboral", "especialidad_forense",
        };

        /// <summary>Ids de los eventos que aparecieron en alguna de esas vidas.</summary>
        public static HashSet<string> SeenEvents(ContentDatabase content, int livesPerArea, int startAge = 25, int finalAge = 60)
        {
            var seen = new HashSet<string>();
            foreach (CareerDefinition career in content.Careers)
            {
                CareerLevelDefinition best = career.Levels.OrderByDescending(l => l.SalaryRelative).First();
                string shortName = career.Id.Substring("carrera.".Length);
                foreach (CareerAreaDefinition area in career.Areas)
                {
                    for (int i = 0; i < livesPerArea; i++)
                    {
                        var session = new LifeSession(content, 500 + (ulong)i);
                        Play(session, startAge);
                        if (!session.World.Player.IsAlive)
                        {
                            continue;
                        }

                        var effects = new List<string>();
                        if (session.World.Player.Studies != null)
                        {
                            effects.Add("jugador.desertar()");
                        }

                        effects.Add($"estudiar('{best.Id}')");
                        effects.Add("jugador.titularse()");
                        effects.AddRange(RequirementFlags.Select(f => $"jugador.marcar('{f}')"));
                        effects.Add("jugador.desmarcar('buscando_trabajo')");
                        effects.Add("jugador.marcar('trabajando')");
                        effects.Add($"jugador.marcar('trabaja_{shortName}')");
                        effects.Add($"jugador.trabajar_en('{area.Id}')");
                        var scope = new EventScope(session.World, content, session.World.Player, null, readOnly: false);
                        foreach (string effect in effects)
                        {
                            Effect.Parse(effect).Apply(scope);
                        }

                        Play(session, finalAge);
                        seen.UnionWith(session.World.EventHistory.Select(r => r.EventId));
                    }
                }
            }

            return seen;
        }

        /// <summary>
        /// Vidas de personas que trabajan con la salud muy baja a los 40 años, para los eventos
        /// de dejar el trabajo por salud y volver al recuperarse, que en una vida al azar le
        /// pasan a cerca del 1 %.
        /// </summary>
        public static HashSet<string> SickWorkerEvents(ContentDatabase content, int lives, int startAge = 40, int finalAge = 55)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < lives; i++)
            {
                var session = new LifeSession(content, 700 + (ulong)i);
                Play(session, startAge);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                var scope = new EventScope(session.World, content, session.World.Player, null, readOnly: false);
                foreach (string effect in new[] { "jugador.desmarcar('buscando_trabajo')", "jugador.marcar('trabajando')", "jugador.trabajar(sueldo_titulo())", "jugador.salud = 30" })
                {
                    Effect.Parse(effect).Apply(scope);
                }

                int before = session.World.EventHistory.Count;
                Play(session, finalAge);
                seen.UnionWith(session.World.EventHistory.Skip(before).Select(r => r.EventId));
            }

            return seen;
        }

        private static void Play(LifeSession session, int age)
        {
            var decider = new AutoDecider(session.Content);
            var chooser = new GameRandom(session.World.Seed).Fork("prueba.areas");
            int target = age * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                PendingEvent? pending = session.Pending;
                if (pending != null)
                {
                    session.Choose(decider.Choose(session.World, pending, chooser));
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }
        }
    }
}

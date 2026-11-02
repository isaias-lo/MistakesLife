using System;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Analysis
{
    /// <summary>
    /// Simula muchas vidas automáticas, eligiendo opciones al azar, y junta
    /// estadísticas. Es reproducible: la misma semilla inicial da el mismo informe.
    /// </summary>
    public sealed class LifeSimulator
    {
        private readonly ContentDatabase _content;

        public LifeSimulator(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>Reglas de semillas a usar (para probar ajustes de equilibrio).</summary>
        public SeedRules SeedRules { get; set; } = new SeedRules();

        /// <summary>Reglas de eventos a usar.</summary>
        public EventRules EventRules { get; set; } = new EventRules();

        public SimulationReport Run(int lives, int finalAge, ulong firstSeed = 1)
        {
            if (lives <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lives));
            }

            var report = new SimulationReport { Lives = lives, FinalAge = finalAge };
            long totalEvents = 0;
            double health = 0, happiness = 0, smarts = 0;

            for (int i = 0; i < lives; i++)
            {
                ulong seed = firstSeed + (ulong)i;
                var session = new LifeSession(_content, seed, SeedRules, EventRules);
                var chooser = new GameRandom(seed).Fork("simulador.decisiones");
                int target = finalAge * GameDate.WeeksPerYear;

                while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
                {
                    PendingEvent? pending = session.Pending;
                    if (pending != null)
                    {
                        session.Choose(chooser.Pick(pending.AvailableOptions));
                        continue;
                    }

                    session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
                }

                var player = session.World.Player;
                if (!player.IsAlive)
                {
                    report.Deaths++;
                }

                health += player.Stats.Health;
                happiness += player.Stats.Happiness;
                smarts += player.Stats.Smarts;

                var lived = session.World.EventHistory.Where(r => r.CharacterId == player.Id).ToList();
                totalEvents += lived.Count;
                foreach (var group in lived.GroupBy(r => r.EventId))
                {
                    report.LivesWithEvent[group.Key] = report.LivesWithEvent.TryGetValue(group.Key, out int n) ? n + 1 : 1;
                    report.EventCount[group.Key] = report.EventCount.TryGetValue(group.Key, out int c) ? c + group.Count() : group.Count();
                }

                foreach (string flag in player.Flags)
                {
                    report.FinalFlags[flag] = report.FinalFlags.TryGetValue(flag, out int f) ? f + 1 : 1;
                }

                foreach (Seed s in session.World.Seeds.All)
                {
                    if (!report.Seeds.TryGetValue(s.TypeId, out SeedStats? stats))
                    {
                        stats = new SeedStats();
                        report.Seeds.Add(s.TypeId, stats);
                    }

                    stats.Planted++;
                    if (s.AwakenedDate.HasValue)
                    {
                        stats.Awakened++;
                    }

                    switch (s.State)
                    {
                        case SeedState.Resolved:
                            stats.Resolved++;
                            break;
                        case SeedState.Expired:
                            stats.Expired++;
                            break;
                        default:
                            stats.Open++;
                            break;
                    }
                }
            }

            report.AverageHealth = health / lives;
            report.AverageHappiness = happiness / lives;
            report.AverageSmarts = smarts / lives;
            report.AverageEventsPerLife = (double)totalEvents / lives;
            report.NeverSeenEvents.AddRange(_content.Events.Select(e => e.Id).Where(id => !report.LivesWithEvent.ContainsKey(id)));
            return report;
        }
    }
}

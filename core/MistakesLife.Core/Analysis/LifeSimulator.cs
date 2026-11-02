using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Analysis
{
    /// <summary>
    /// Simula muchas vidas automáticas, decidiendo según la tendencia de cada opción
    /// (personalidad, hogar, historia), y junta
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
            double health = 0, happiness = 0, smarts = 0, inflation = 0;
            int inflationLives = 0;
            var realSalaries = new List<double>();
            var realMoney = new List<double>();
            var realPensions = new List<double>();
            var realDebts = new List<double>();

            for (int i = 0; i < lives; i++)
            {
                ulong seed = firstSeed + (ulong)i;
                var session = new LifeSession(_content, seed, SeedRules, EventRules);
                var chooser = new GameRandom(seed).Fork("simulador.decisiones");
                var decider = new AutoDecider(_content);
                int target = finalAge * GameDate.WeeksPerYear;

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

                var player = session.World.Player;
                if (!player.IsAlive)
                {
                    report.Deaths++;
                }

                var economy = player.Residence == null ? null : session.World.EconomyOf(player.Residence.CountryId);
                if (economy != null)
                {
                    double years = (double)player.AgeInWeeks(session.World.Date) / GameDate.WeeksPerYear;
                    if (years > 0)
                    {
                        inflation += Math.Pow(economy.PriceIndex, 1 / years) - 1;
                        inflationLives++;
                    }

                    realMoney.Add((double)player.Money / economy.PriceIndex);
                    if (player.Debts.Count > 0)
                    {
                        report.WithDebt++;
                        realDebts.Add((double)player.Debts.Sum(d => d.Balance) / economy.PriceIndex);
                    }

                    if (player.Delinquent)
                    {
                        report.Delinquent++;
                    }

                    if (player.Pension.HasValue)
                    {
                        report.Retired++;
                        realPensions.Add((double)player.LastPensionPayment / economy.PriceIndex);
                    }

                    if (player.Job != null)
                    {
                        realSalaries.Add((double)player.Job.Salary / economy.PriceIndex);
                    }
                }

                if (player.Job != null)
                {
                    report.Working++;
                }

                if (player.Money < 0)
                {
                    report.InRed++;
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

                foreach (string degree in player.Degrees)
                {
                    report.Degrees[degree] = report.Degrees.TryGetValue(degree, out int d) ? d + 1 : 1;
                }

                if (player.Studies != null)
                {
                    string level = player.Studies.LevelId;
                    report.StillStudying[level] = report.StillStudying.TryGetValue(level, out int st) ? st + 1 : 1;
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
            report.AverageAnnualInflation = inflationLives == 0 ? 0 : inflation / inflationLives;
            realSalaries.Sort();
            report.MedianRealSalary = realSalaries.Count == 0 ? 0 : realSalaries[realSalaries.Count / 2];
            realDebts.Sort();
            report.MedianRealDebt = realDebts.Count == 0 ? 0 : realDebts[realDebts.Count / 2];
            realPensions.Sort();
            report.MedianRealPension = realPensions.Count == 0 ? 0 : realPensions[realPensions.Count / 2];
            realMoney.Sort();
            report.MedianRealMoney = realMoney.Count == 0 ? 0 : realMoney[realMoney.Count / 2];
            report.NeverSeenEvents.AddRange(_content.Events.Select(e => e.Id).Where(id => !report.LivesWithEvent.ContainsKey(id)));
            return report;
        }
    }
}

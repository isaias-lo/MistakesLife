using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
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

        /// <summary>
        /// Marca con que los paquetes dicen que alguien busca trabajo. Solo sirve
        /// para contar el desempleo en el informe.
        /// </summary>
        public string JobSearchFlag { get; set; } = "buscando_trabajo";

        /// <summary>Marca que pone el contenido al llegar a la cima de una carrera.</summary>
        public string TopFlag { get; set; } = "llego_a_la_cima";

        /// <summary>Edad hasta la que se mide la densidad de cotización (meses cotizados desde los 18).</summary>
        public const int ContributionDensityAge = 65;

        public SimulationReport Run(int lives, int finalAge, ulong firstSeed = 1) => Run(lives, finalAge, null, firstSeed);

        /// <summary>Simula vidas que nacen en un país elegido (null: al azar, como en el juego).</summary>
        public SimulationReport Run(int lives, int finalAge, string? countryId, ulong firstSeed = 1)
        {
            if (lives <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lives));
            }

            var report = new SimulationReport { Lives = lives, FinalAge = finalAge };
            long totalEvents = 0;
            double health = 0, happiness = 0, smarts = 0, inflation = 0;
            int inflationLives = 0;
            double density = 0;
            int densityLives = 0;
            int totalFriends = 0;
            int promotions = 0;
            double motherAffection = 0;
            int mothers = 0;
            double partnerResentment = 0;
            int partners = 0;
            var realSalaries = new List<double>();
            var realMoney = new List<double>();
            var realPensions = new List<double>();
            var realDebts = new List<double>();
            long searchWeeks = 0, searchInterviews = 0;

            for (int i = 0; i < lives; i++)
            {
                ulong seed = firstSeed + (ulong)i;
                var session = new LifeSession(_content, seed, countryId, SeedRules, EventRules);
                session.TimeMachine.Enabled = false; // sin fotos del mundo, para ir rápido
                var chooser = new GameRandom(seed).Fork("simulador.decisiones");
                var decider = new AutoDecider(_content);
                int target = finalAge * GameDate.WeeksPerYear;

                while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
                {
                    PendingEvent? pending = session.Pending;
                    if (pending != null)
                    {
                        JobSearch? search = session.World.Player.JobSearch;
                        int weeks = search?.Weeks ?? 0, interviews = search?.Interviews ?? 0;
                        session.Choose(decider.Choose(session.World, pending, chooser));
                        if (search != null && session.World.Player.JobSearch == null && session.World.Player.Job != null)
                        {
                            // Encontró trabajo buscándolo: cuánto demoró y a cuántas entrevistas fue.
                            report.JobSearchHires++;
                            searchWeeks += weeks;
                            searchInterviews += interviews;
                        }

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

                    // Al morir, el dinero y las deudas pasan a la herencia: se miden solo en vidas que siguen.
                    if (player.IsAlive)
                    {
                        realMoney.Add((double)player.Money / economy.PriceIndex);
                    }

                    if (player.Debts.Count > 0)
                    {
                        report.WithDebt++;
                        realDebts.Add((double)player.Debts.Sum(d => d.Balance) / economy.PriceIndex);
                    }

                    if (player.IsAlive && player.Delinquent)
                    {
                        report.Delinquent++;
                    }

                    if (player.Phone != null)
                    {
                        report.WithPhone++;
                    }

                    if (player.Mail != null)
                    {
                        report.WithMail++;
                        report.MailsReceived += player.Mail.Messages.Count;
                    }

                    if (session.World.Journal.Any(j => j.CharacterId == player.Id && j.TextKey == "diario.correo.estafa"))
                    {
                        report.ScamVictims++;
                    }

                    if (player.IsAlive && player.Pension.HasValue)
                    {
                        report.Retired++;
                        realPensions.Add((double)player.LastPensionPayment / economy.PriceIndex);
                    }

                    if (player.IsAlive && player.Job != null)
                    {
                        realSalaries.Add((double)player.Job.Salary / economy.PriceIndex);
                    }
                }

                if (player.Job?.Position != null)
                {
                    string positionId = player.Job.Position.PositionId;
                    report.FinalPositions[positionId] = report.FinalPositions.TryGetValue(positionId, out int count) ? count + 1 : 1;
                    promotions += _content.GetCareerPosition(positionId).Depth;
                }

                Company? company = session.World.CompanyById(player.Job?.CompanyId);
                if (company != null)
                {
                    report.FinalCompanySizes[company.SizeId] = report.FinalCompanySizes.TryGetValue(company.SizeId, out int sized) ? sized + 1 : 1;
                }

                report.CompaniesClosed += session.World.Companies.Count(c => !c.IsOpen);

                if (player.HasFlag(TopFlag))
                {
                    report.ReachedTop++;
                }

                if (player.IsAlive && player.Studies != null)
                {
                    report.Studying++;
                }

                if (player.IsAlive && player.Job != null)
                {
                    report.Working++;
                    if (player.Job.Kind == JobKind.Informal)
                    {
                        report.WorkingInformal++;
                    }
                }
                else if (player.IsAlive && player.HasFlag(JobSearchFlag))
                {
                    report.Unemployed++;
                }

                if (player.AgeInYears(session.World.Date) >= ContributionDensityAge)
                {
                    const int months = (ContributionDensityAge - 18) * 12;
                    density += Math.Min(1.0, (double)player.ContributedMonths / months);
                    densityLives++;
                }

                if (player.IsAlive && player.Money < 0)
                {
                    report.InRed++;
                }

                if (player.IsAlive)
                {
                    int friends = RelationshipRules.WithBond(session.World, player, RelationshipBond.Friend).Count;
                    totalFriends += friends;
                    report.WithFriends += friends > 0 ? 1 : 0;
                    report.WithRival += RelationshipRules.WithBond(session.World, player, RelationshipBond.Rival).Count > 0 ? 1 : 0;
                    if (player.MotherId.HasValue && session.World.TryGetCharacter(player.MotherId.Value, out Character? mother) && mother.IsAlive)
                    {
                        motherAffection += RelationshipRules.Feelings(session.World, mother, player).Affection;
                        mothers++;
                    }

                    if (player.PartnerId.HasValue && session.World.TryGetCharacter(player.PartnerId.Value, out Character? partner))
                    {
                        partnerResentment += RelationshipRules.Feelings(session.World, partner, player).Resentment;
                        partners++;
                    }
                }

                report.WithEx += player.Relationships.Any(r => r.Bond == RelationshipBond.Ex) ? 1 : 0;

                health += player.Stats.Health;
                happiness += player.Stats.Happiness;
                smarts += player.Stats.Smarts;

                foreach (var news in session.World.News)
                {
                    report.NewsCount[news.NewsId] = report.NewsCount.TryGetValue(news.NewsId, out int k) ? k + 1 : 1;
                }

                foreach (var courtCase in session.World.CourtCases.Where(c => c.OwnerId == player.Id))
                {
                    string key = courtCase.TypeId + " → " + (courtCase.Outcome ?? "abierta");
                    report.CourtCaseCount[key] = report.CourtCaseCount.TryGetValue(key, out int c) ? c + 1 : 1;
                }

                report.UnitedAtEnd += player.UnionTypeId != null ? 1 : 0;

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
            report.AverageContributionDensity = densityLives == 0 ? 0 : density / densityLives;
            report.AverageFriends = lives == 0 ? 0 : (double)totalFriends / lives;
            report.AverageSearchWeeks = report.JobSearchHires == 0 ? 0 : (double)searchWeeks / report.JobSearchHires;
            report.AverageInterviewsPerHire = report.JobSearchHires == 0 ? 0 : (double)searchInterviews / report.JobSearchHires;
            int inCareer = report.FinalPositions.Values.Sum();
            report.AveragePromotions = inCareer == 0 ? 0 : (double)promotions / inCareer;
            report.AverageMotherAffection = mothers == 0 ? 0 : motherAffection / mothers;
            report.AveragePartnerResentment = partners == 0 ? 0 : partnerResentment / partners;
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

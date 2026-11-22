using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Saving
{
    /// <summary>Copia el estado de un mundo en los datos que se guardan.</summary>
    internal static class WorldWriter
    {
        public static WorldData Write(World world)
        {
            var data = new WorldData
            {
                Seed = world.Seed,
                RandomState = world.Random.State,
                Week = world.Date.TotalWeeks,
                PlayerId = world.PlayerId,
                NextCharacterId = world.NextCharacterIdValue,
                FormerPlayers = world.FormerPlayers.ToList(),
                Characters = world.Characters.Select(WriteCharacter).ToList(),
                NextSeedId = world.Seeds.NextId,
                SeedAwakenings = world.Seeds.Awakenings.Select(d => d.TotalWeeks).ToList(),
                Seeds = world.Seeds.All.Select(WriteSeed).ToList(),
                Economies = world.Economies.Select(WriteEconomy).ToList(),
                Companies = world.Companies.Select(co => new CompanyData
                {
                    Id = co.Id,
                    CountryId = co.CountryId,
                    CareerId = co.CareerId,
                    SizeId = co.SizeId,
                    NameId = co.NameId,
                    FoundedWeek = co.Founded.TotalWeeks,
                    ClosedWeek = co.ClosedDate?.TotalWeeks,
                }).ToList(),
                NextCompanyId = world.NextCompanyIdValue,
                LawChanges = world.LawChanges.OrderBy(l => l.Key, System.StringComparer.Ordinal).Select(l => WriteLaw(l.Key, l.Value)).ToList(),
                Journal = world.Journal.Select(j => new JournalData { Week = j.Date.TotalWeeks, CharacterId = j.CharacterId, TextKey = j.TextKey }).ToList(),
                News = world.News.Select(n => new NewsData { Week = n.Date.TotalWeeks, NewsId = n.NewsId, CountryId = n.CountryId, CityId = n.CityId, TextKey = n.TextKey }).ToList(),
                CourtCases = world.CourtCases.Select(c => new CourtCaseData
                {
                    Id = c.Id, TypeId = c.TypeId, Rit = c.Rit, OwnerId = c.OwnerId, OtherPartyId = c.OtherPartyId, OpenedWeek = c.Opened.TotalWeeks,
                    StageIndex = c.StageIndex, StageStartedWeek = c.StageStarted.TotalWeeks, ClosedWeek = c.Closed?.TotalWeeks, Outcome = c.Outcome, Amount = c.Amount,
                }).ToList(),
                PendingEvents = world.PendingEvents.Select(p => new PendingEventData
                {
                    EventId = p.EventId,
                    CharacterId = p.CharacterId,
                    Week = p.Date.TotalWeeks,
                    SeedId = p.SeedId,
                    PersonId = p.PersonId,
                    AvailableOptions = p.AvailableOptions.ToList(),
                }).ToList(),
                EventHistory = world.EventHistory.Select(e => new EventRecordData
                {
                    Week = e.Date.TotalWeeks,
                    EventId = e.EventId,
                    CharacterId = e.CharacterId,
                    ChosenOption = e.ChosenOption,
                    PersonId = e.PersonId,
                }).ToList(),
            };
            return data;
        }

        private static CharacterData WriteCharacter(Character c)
        {
            return new CharacterData
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Sex = c.Sex.ToString(),
                Gender = c.Gender.ToString(),
                GenderIdentity = c.GenderIdentity.ToString(),
                Orientation = c.Orientation.ToString(),
                BirthWeek = c.BirthDate.TotalWeeks,
                Personality = new List<int>
                {
                    c.Personality.Openness,
                    c.Personality.Conscientiousness,
                    c.Personality.Extraversion,
                    c.Personality.Agreeableness,
                    c.Personality.Neuroticism,
                },
                Health = c.Stats.Health,
                Happiness = c.Stats.Happiness,
                Smarts = c.Stats.Smarts,
                Energy = c.Stats.Energy,
                Looks = c.Stats.Looks,
                InheritedLooks = c.InheritedLooks,
                MotherId = c.MotherId,
                FatherId = c.FatherId,
                SecondParentId = c.SecondParentId,
                Adopted = c.Adopted,
                PartnerId = c.PartnerId,
                SpouseId = c.SpouseId,
                UnionTypeId = c.UnionTypeId,
                LivesWithId = c.LivesWithId,
                SharedCustody = c.SharedCustody,
                CountryId = c.Residence?.CountryId,
                CityId = c.Residence?.CityId,
                CommuneId = c.Residence?.CommuneId,
                HouseholdLevel = c.HouseholdLevel,
                Money = c.Money,
                Job = JobOf(c.Job),
                LastJob = JobOf(c.LastJob),
                PendingInheritance = c.PendingInheritance,
                Venture = c.Venture == null ? null : new VentureData
                {
                    PartnerId = c.Venture.PartnerId,
                    Formal = c.Venture.Formal,
                    Contribution = c.Venture.Contribution,
                    MonthsOpen = c.Venture.MonthsOpen,
                    LastShare = c.Venture.LastShare,
                    TotalShare = c.Venture.TotalShare,
                    Luck = c.Venture.Luck,
                },
                JobSearch = c.JobSearch == null ? null : new JobSearchData
                {
                    Target = c.JobSearch.Target,
                    Kind = c.JobSearch.Kind.ToString(),
                    MethodId = c.JobSearch.MethodId,
                    ContactId = c.JobSearch.ContactId,
                    Stage = c.JobSearch.Stage.ToString(),
                    Weeks = c.JobSearch.Weeks,
                    Applications = c.JobSearch.Applications,
                    Interviews = c.JobSearch.Interviews,
                    Rejections = c.JobSearch.Rejections,
                    Negotiated = c.JobSearch.Negotiated,
                    Raise = c.JobSearch.Raise,
                    CompanyNameId = c.JobSearch.CompanyNameId,
                    LastCompanyNameId = c.JobSearch.LastCompanyNameId,
                },
                Phone = c.Phone == null ? null : new PhoneData
                {
                    SinceWeek = c.Phone.Since.TotalWeeks,
                    Apps = c.Phone.Apps.ToList(),
                },
                Mail = c.Mail == null ? null : new MailData
                {
                    SinceWeek = c.Mail.Since.TotalWeeks,
                    JournalCursor = c.Mail.JournalCursor,
                    Messages = c.Mail.Messages.Select(m => new MailMessageData { Week = m.Date.TotalWeeks, MailId = m.MailId, Read = m.Read }).ToList(),
                },
                Chats = c.Chats == null ? null : new ChatInboxData
                {
                    JournalCursor = c.Chats.JournalCursor,
                    Chats = c.Chats.Chats.Select(chat => new ChatData
                    {
                        OtherId = chat.OtherId,
                        Unread = chat.Unread,
                        AwaitingReply = chat.AwaitingReply,
                        AwaitingSinceWeek = chat.AwaitingSince.TotalWeeks,
                        PlayerWrote = chat.PlayerWrote,
                        PlayerWroteWeek = chat.PlayerWroteOn.TotalWeeks,
                        Messages = chat.Messages.Select(m => new ChatMessageData { Week = m.Date.TotalWeeks, TextKey = m.TextKey, FromPlayer = m.FromPlayer }).ToList(),
                        LimitedSent = chat.LimitedSent.Count == 0 ? null : chat.LimitedSent.OrderBy(p => p.Key, System.StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value),
                    }).ToList(),
                },
                LastAreaId = c.LastPosition?.AreaId,
                LastPositionId = c.LastPosition?.PositionId,
                LastPayslip = c.LastPayslip == null ? null : new PayslipData
                {
                    Week = c.LastPayslip.Date.TotalWeeks,
                    Kind = c.LastPayslip.Kind.ToString(),
                    Gross = c.LastPayslip.Gross,
                    Taxable = c.LastPayslip.Taxable,
                    Lines = WriteLines(c.LastPayslip.Lines),
                },
                Incomes = c.Incomes.Select(i => new IncomeData
                {
                    Year = i.Year,
                    EmployeeTaxable = i.EmployeeTaxable,
                    TaxWithheld = i.TaxWithheld,
                    FreelanceGross = i.FreelanceGross,
                    FreelanceRetention = i.FreelanceRetention,
                }).ToList(),
                LastExpenses = WriteLines(c.LastExpenses),
                MonthsInRed = c.MonthsInRed,
                Lease = c.Lease == null ? null : new LeaseData
                {
                    TypeId = c.Lease.TypeId,
                    CommuneId = c.Lease.CommuneId,
                    BaseRent = c.Lease.BaseRent,
                    AdjustedPriceIndex = c.Lease.AdjustedPriceIndex,
                    MonthsSinceAdjustment = c.Lease.MonthsSinceAdjustment,
                    MonthsLeft = c.Lease.MonthsLeft,
                    Deposit = c.Lease.Deposit,
                    RoommateId = c.Lease.RoommateId,
                },
                GroceryBasketId = c.GroceryBasketId,
                Services = new Dictionary<string, string>(c.Services),
                HealthPlanId = c.HealthPlanId,
                LastHealthCareWeek = c.LastHealthCareDate?.TotalWeeks,
                PendingTaxReturn = c.PendingTaxReturn == null ? null : new TaxReturnData
                {
                    Year = c.PendingTaxReturn.Year,
                    TaxableIncome = c.PendingTaxReturn.TaxableIncome,
                    Tax = c.PendingTaxReturn.Tax,
                    Contributions = c.PendingTaxReturn.Contributions,
                    Credits = c.PendingTaxReturn.Credits,
                },
                PensionFund = c.PensionFund,
                ContributedMonths = c.ContributedMonths,
                PensionSalaryBase = c.PensionSalaryBase,
                Pension = c.Pension,
                ReceivesUniversalPension = c.ReceivesUniversalPension,
                LastPensionPayment = c.LastPensionPayment,
                UnemploymentFund = c.UnemploymentFund,
                UnemploymentContributions = c.UnemploymentContributions,
                UnemploymentSalaryBase = c.UnemploymentSalaryBase,
                Griefs = c.Griefs.Count == 0 ? null : c.Griefs.Select(g => new GriefData
                {
                    PersonId = g.PersonId,
                    SinceWeek = g.Since.TotalWeeks,
                    Relation = g.Relation,
                    Weight = g.Weight,
                    ReliefWeeks = g.ReliefWeeks,
                }).ToList(),
                UnemploymentClaim = c.UnemploymentClaim == null ? null : new UnemploymentClaimData
                {
                    StartWeek = c.UnemploymentClaim.Start.TotalWeeks,
                    HasRight = c.UnemploymentClaim.HasRight,
                    Solidarity = c.UnemploymentClaim.Solidarity,
                    SalaryBase = c.UnemploymentClaim.SalaryBase,
                    Finished = c.UnemploymentClaim.Finished,
                    Payments = c.UnemploymentClaim.Payments.Select(p => new UnemploymentPaymentData
                    {
                        Week = p.Date.TotalWeeks,
                        Number = p.Number,
                        Amount = p.Amount,
                        FromSolidarityFund = p.FromSolidarityFund,
                    }).ToList(),
                },
                Debts = c.Debts.Select(d => new DebtData
                {
                    TypeId = d.TypeId,
                    Balance = d.Balance,
                    RemainingInstallments = d.RemainingInstallments,
                    MonthsLate = d.MonthsLate,
                    PriceIndex = d.PriceIndex,
                }).ToList(),
                LastDebtPayment = c.LastDebtPayment,
                Delinquent = c.Delinquent,
                Relationships = c.Relationships.Select(r => new RelationshipData
                {
                    OtherId = r.OtherId,
                    Affection = r.Affection,
                    Trust = r.Trust,
                    Respect = r.Respect,
                    Resentment = r.Resentment,
                    Bond = r.Bond.ToString(),
                }).ToList(),
                EducationStageId = c.EducationStageId,
                Flags = c.Flags.ToList(),
                WorkStatus = c.WorkStatus?.ToString(),
                CoupleStage = c.CoupleStage?.ToString(),
                CoupleSinceWeek = c.CoupleSince?.TotalWeeks,
                LatePartnerId = c.LatePartnerId,
                InterestingPeople = c.InterestingPeople.Count == 0 ? null : c.InterestingPeople
                    .Select(p => new InterestingPersonData { PersonId = p.PersonId, Source = p.Source, SinceWeek = p.Since.TotalWeeks, Declines = p.Declines })
                    .ToList(),
                Studies = c.Studies == null ? null : new StudiesData
                {
                    CareerId = c.Studies.CareerId,
                    LevelId = c.Studies.LevelId,
                    Institution = c.Studies.Institution,
                    RequiredSemesters = c.Studies.RequiredSemesters,
                    CompletedSemesters = c.Studies.CompletedSemesters,
                    WeeksIntoSemester = c.Studies.WeeksIntoSemester,
                    GradedSemesters = c.Studies.GradedSemesters,
                    Preparation = c.Studies.Preparation,
                    LastMarksSemester = c.Studies.LastMarksSemester,
                    SemestersTaken = c.Studies.SemestersTaken,
                    LastMarks = new SortedDictionary<string, double>(c.Studies.LastMarks),
                    SemesterAverages = c.Studies.SemesterAverages.ToList(),
                    ProjectDeliveries = new SortedDictionary<string, List<int>>(c.Studies.ProjectDeliveries.ToDictionary(p => p.Key, p => p.Value.ToList()), System.StringComparer.Ordinal),
                },
                Routine = c.Routine == null ? null : new RoutineData
                {
                    StageKey = c.Routine.StageKey,
                    Confirmed = c.Routine.Confirmed,
                    Slots = c.Routine.Slots.ToList(),
                },
                FreeTime = c.FreeTime == null ? null : new FreeTimeData
                {
                    MonthKey = c.FreeTime.MonthKey,
                    Used = c.FreeTime.Used,
                    Done = c.FreeTime.Done.ToList(),
                },
                OwnRules = c.OwnRules.Count == 0 ? null : c.OwnRules.Select(r => new OwnRuleData
                {
                    Key = r.Key, Choice = r.Choice, TimesUsed = r.TimesUsed, LastUsedWeek = r.LastUsed?.TotalWeeks,
                }).ToList(),
                Wishes = c.Wishes.Count == 0 ? null : c.Wishes.Select(w => new WishData
                {
                    WishId = w.WishId, StageKey = w.StageKey, SinceWeek = w.Since.TotalWeeks, PersonId = w.PersonId,
                    Highlighted = w.Highlighted, State = (int)w.State, ClosedWeek = w.Closed?.TotalWeeks,
                }).ToList(),
                SchoolClass = c.SchoolClass == null ? null : new SchoolClassData
                {
                    StageId = c.SchoolClass.StageId, Level = c.SchoolClass.Level, Letter = c.SchoolClass.Letter,
                    SchoolYear = c.SchoolClass.SchoolYear, Students = c.SchoolClass.Students,
                    TeacherId = c.SchoolClass.TeacherId, ClassmateIds = c.SchoolClass.ClassmateIds.ToList(), LeftIds = c.SchoolClass.LeftIds.ToList(),
                },
                Team = c.Team == null ? null : new WorkTeamData
                {
                    BossId = c.Team.BossId, ColleagueIds = c.Team.ColleagueIds.ToList(), LeftIds = c.Team.LeftIds.ToList(), MonthsInJob = c.Team.MonthsInJob,
                    RecommendedIds = c.Team.RecommendedIds.ToList(),
                },
                SchoolRecord = c.SchoolRecord == null ? null : new SchoolRecordData
                {
                    Marks = c.SchoolRecord.Marks.Count == 0 ? null : c.SchoolRecord.Marks.ToDictionary(m => m.Key, m => m.Value.ToList()),
                    YearAverages = c.SchoolRecord.YearAverages.Count == 0 ? null : c.SchoolRecord.YearAverages.ToList(),
                    LastFinals = c.SchoolRecord.LastFinals.Count == 0 ? null : c.SchoolRecord.LastFinals.ToDictionary(f => f.Key, f => f.Value),
                    RepeatedYears = c.SchoolRecord.RepeatedYears,
                    TestSubjectId = c.SchoolRecord.TestSubjectId,
                    TestWeek = c.SchoolRecord.TestWeek,
                    TestPreparation = c.SchoolRecord.TestPreparation,
                    TestMark = c.SchoolRecord.TestMark,
                    TestReturnedWeek = c.SchoolRecord.TestReturnedWeek,
                    TestMakeup = c.SchoolRecord.TestMakeup,
                    TestCheating = c.SchoolRecord.TestCheating,
                    TestCaught = c.SchoolRecord.TestCaught,
                    Absences = c.SchoolRecord.Absences,
                    WorkshopId = c.SchoolRecord.WorkshopId,
                    CouncilRole = c.SchoolRecord.CouncilRole,
                    CouncilVoteWeek = c.SchoolRecord.CouncilVoteWeek,
                    CouncilResult = c.SchoolRecord.CouncilResult,
                    Mastery = c.SchoolRecord.Mastery.Count == 0 ? null : c.SchoolRecord.Mastery.ToDictionary(m => m.Key, m => m.Value),
                    LastMastery = c.SchoolRecord.LastMastery.Count == 0 ? null : c.SchoolRecord.LastMastery.ToDictionary(m => m.Key, m => m.Value),
                    MasteryYear = c.SchoolRecord.MasteryYear,
                    LastStudiedWeek = c.SchoolRecord.LastStudiedWeek.Count == 0 ? null : c.SchoolRecord.LastStudiedWeek.ToDictionary(m => m.Key, m => m.Value),
                    SeenQuestions = c.SchoolRecord.SeenQuestions.Count == 0 ? null : c.SchoolRecord.SeenQuestions.ToList(),
                    StudySessions = c.SchoolRecord.StudySessions,
                    AdmissionNem = c.SchoolRecord.AdmissionNem,
                    AdmissionRanking = c.SchoolRecord.AdmissionRanking,
                    AdmissionTests = c.SchoolRecord.AdmissionTests.Count == 0 ? null : c.SchoolRecord.AdmissionTests.ToDictionary(m => m.Key, m => m.Value),
                    LastAdmissionTests = c.SchoolRecord.LastAdmissionTests.Count == 0 ? null : c.SchoolRecord.LastAdmissionTests.ToDictionary(m => m.Key, m => m.Value),
                    AdmissionAttempts = c.SchoolRecord.AdmissionAttempts,
                },
                Degrees = c.Degrees.ToList(),
                IsAlive = c.IsAlive,
                DeathWeek = c.DeathDate?.TotalWeeks,
                DeathCauseId = c.DeathCauseId,
            };
        }

        private static List<MoneyLineData> WriteLines(IEnumerable<PayslipLine> lines) =>
            lines.Select(l => new MoneyLineData { Id = l.Id, Amount = l.Amount }).ToList();

        private static SeedData WriteSeed(Seed s)
        {
            return new SeedData
            {
                Id = s.Id,
                TypeId = s.TypeId,
                Polarity = s.Polarity.ToString(),
                OwnerId = s.OwnerId,
                PlantedWeek = s.PlantedDate.TotalWeeks,
                OwnerAgeAtPlanting = s.OwnerAgeAtPlanting,
                ParentSeedId = s.ParentSeedId,
                ExpiresAfterWeeks = s.ExpiresAfterWeeks,
                MinWeeksDormant = s.MinWeeksDormant,
                Heritable = s.Heritable,
                PossibleEvents = s.PossibleEvents.ToList(),
                Witnesses = s.Witnesses.ToList(),
                Strength = s.Strength,
                State = s.State.ToString(),
                AwakenedWeek = s.AwakenedDate?.TotalWeeks,
                ClosedWeek = s.ClosedDate?.TotalWeeks,
                OriginEventId = s.Origin?.EventId,
                OriginOption = s.Origin?.OptionTextKey,
                OriginPlace = s.Origin?.PlaceKey,
                OriginPersonId = s.Origin?.PersonId,
            };
        }

        private static EconomyData WriteEconomy(CountryEconomy e)
        {
            return new EconomyData
            {
                CountryId = e.CountryId,
                RegimeId = e.RegimeId,
                PhaseId = e.PhaseId,
                AnnualInflation = e.AnnualInflation,
                PriceIndex = e.PriceIndex,
                IndexHistory = e.IndexHistory.ToList(),
            };
        }

        private static LawChangeData WriteLaw(string key, Value value)
        {
            int separator = key.IndexOf('|');
            var data = new LawChangeData { CountryId = key.Substring(0, separator), LawId = key.Substring(separator + 1) };
            switch (value.Kind)
            {
                case ValueKind.Bool:
                    data.Kind = "bool";
                    data.Bool = value.AsBool("ley");
                    break;
                case ValueKind.Number:
                    data.Kind = "number";
                    data.Number = value.AsNumber("ley");
                    break;
                case ValueKind.Text:
                    data.Kind = "text";
                    data.Text = value.AsText("ley");
                    break;
                default:
                    throw new System.InvalidOperationException($"La ley '{data.LawId}' tiene un valor que no se puede guardar ({value.Kind}).");
            }

            return data;
        }

        private static JobData? JobOf(Job? job) => job == null ? null : new JobData
        {
            Kind = job.Kind.ToString(),
            BaseSalary = job.BaseSalary,
            AdjustedPriceIndex = job.AdjustedPriceIndex,
            MonthsSinceAdjustment = job.MonthsSinceAdjustment,
            AreaId = job.Position?.AreaId,
            PositionId = job.Position?.PositionId,
            MonthsInPosition = job.MonthsInPosition,
            MonthsInJob = job.MonthsInJob,
            Performance = job.Performance,
            GoalPercent = job.GoalPercent,
            MonthsUnderGoal = job.MonthsUnderGoal,
            MonthsOnGoal = job.MonthsOnGoal,
            Warnings = job.Warnings,
            VariablePay = job.VariablePay,
            Tips = job.Tips,
            Challenge = job.Challenge,
            VacationDays = job.VacationDays,
            LeavingWeek = job.LeavingWeek,
            CompanyId = job.CompanyId,
        };
    }
}

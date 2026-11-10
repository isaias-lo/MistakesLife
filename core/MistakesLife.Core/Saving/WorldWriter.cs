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
                PendingEvents = world.PendingEvents.Select(p => new PendingEventData
                {
                    EventId = p.EventId,
                    CharacterId = p.CharacterId,
                    Week = p.Date.TotalWeeks,
                    SeedId = p.SeedId,
                    AvailableOptions = p.AvailableOptions.ToList(),
                }).ToList(),
                EventHistory = world.EventHistory.Select(e => new EventRecordData
                {
                    Week = e.Date.TotalWeeks,
                    EventId = e.EventId,
                    CharacterId = e.CharacterId,
                    ChosenOption = e.ChosenOption,
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
                MotherId = c.MotherId,
                FatherId = c.FatherId,
                SecondParentId = c.SecondParentId,
                Adopted = c.Adopted,
                PartnerId = c.PartnerId,
                CountryId = c.Residence?.CountryId,
                CityId = c.Residence?.CityId,
                CommuneId = c.Residence?.CommuneId,
                HouseholdLevel = c.HouseholdLevel,
                Money = c.Money,
                Job = c.Job == null ? null : new JobData
                {
                    Kind = c.Job.Kind.ToString(),
                    BaseSalary = c.Job.BaseSalary,
                    AdjustedPriceIndex = c.Job.AdjustedPriceIndex,
                    MonthsSinceAdjustment = c.Job.MonthsSinceAdjustment,
                    AreaId = c.Job.Position?.AreaId,
                    PositionId = c.Job.Position?.PositionId,
                    MonthsInPosition = c.Job.MonthsInPosition,
                    Performance = c.Job.Performance,
                    CompanyId = c.Job.CompanyId,
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
                },
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
                Studies = c.Studies == null ? null : new StudiesData
                {
                    CareerId = c.Studies.CareerId,
                    LevelId = c.Studies.LevelId,
                    Institution = c.Studies.Institution,
                    RequiredSemesters = c.Studies.RequiredSemesters,
                    CompletedSemesters = c.Studies.CompletedSemesters,
                    WeeksIntoSemester = c.Studies.WeeksIntoSemester,
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
    }
}

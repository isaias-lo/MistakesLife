using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Saving
{
    /// <summary>Arma un mundo a partir de los datos guardados.</summary>
    internal static class WorldReader
    {
        public static World Read(WorldData data, ContentDatabase content)
        {
            var world = new World(data.Seed, Date(data.Week), data.RandomState);

            // Los personajes se agregan en el mismo orden en que se guardaron:
            // los sistemas los recorren en ese orden, y así la vida sigue igual.
            foreach (CharacterData c in data.Characters)
            {
                world.AddCharacter(ReadCharacter(c));
            }

            if (!world.TryGetCharacter(data.PlayerId, out _))
            {
                throw new SaveFormatException($"La partida guardada no tiene al jugador (personaje {data.PlayerId}).");
            }

            world.RestoreState(data.PlayerId, data.NextCharacterId, data.FormerPlayers);

            foreach (SeedData s in data.Seeds)
            {
                ReadSeed(world.Seeds, s, content);
            }

            world.Seeds.RestoreCounters(data.NextSeedId, data.SeedAwakenings.Select(Date));

            foreach (CompanyData co in data.Companies)
            {
                var company = new Company(co.Id, co.CountryId, co.CareerId, co.SizeId, co.NameId, Date(co.FoundedWeek))
                {
                    ClosedDate = co.ClosedWeek.HasValue ? Date(co.ClosedWeek.Value) : (GameDate?)null,
                };
                world.RestoreCompany(company);
            }

            world.NextCompanyIdValue = System.Math.Max(world.NextCompanyIdValue, data.NextCompanyId);

            foreach (EconomyData e in data.Economies)
            {
                var economy = new CountryEconomy(e.CountryId, e.RegimeId, e.AnnualInflation) { PhaseId = e.PhaseId };
                economy.Restore(e.PriceIndex, e.IndexHistory);
                world.AddEconomy(economy);
            }

            foreach (LawChangeData law in data.LawChanges)
            {
                world.RestoreLawChange(law.CountryId + "|" + law.LawId, ReadLaw(law));
            }

            foreach (JournalData j in data.Journal)
            {
                world.RestoreJournal(new JournalEntry(Date(j.Week), j.CharacterId, j.TextKey));
            }

            foreach (NewsData n in data.News)
            {
                world.AddNews(new NewsRecord(Date(n.Week), n.NewsId, n.CountryId, n.CityId, n.TextKey));
            }

            foreach (PendingEventData p in data.PendingEvents)
            {
                // Un evento que ya no está en los paquetes (se retiró) no se puede
                // mostrar: se descarta y la vida sigue.
                if (!content.TryGetEvent(p.EventId, out _))
                {
                    continue;
                }

                world.AddPendingEvent(new PendingEvent(p.EventId, p.CharacterId, Date(p.Week), p.SeedId, p.AvailableOptions.ToList()));
            }

            foreach (EventRecordData e in data.EventHistory)
            {
                world.RecordEvent(new EventRecord(Date(e.Week), e.EventId, e.CharacterId, e.ChosenOption));
            }

            return world;
        }

        private static Character ReadCharacter(CharacterData d)
        {
            if (d.Personality.Count != 5)
            {
                throw new SaveFormatException($"El personaje {d.Id} no tiene sus cinco rasgos de personalidad.");
            }

            var p = d.Personality;
            var character = new Character(
                d.Id,
                d.FirstName,
                d.LastName,
                Parse<Sex>(d.Sex),
                Date(d.BirthWeek),
                new Personality(p[0], p[1], p[2], p[3], p[4]),
                new Stats(d.Health, d.Happiness, d.Smarts))
            {
                Gender = Parse<Gender>(d.Gender),
                GenderIdentity = Parse<Gender>(d.GenderIdentity),
                Orientation = Parse<Orientation>(d.Orientation),
                MotherId = d.MotherId,
                FatherId = d.FatherId,
                SecondParentId = d.SecondParentId,
                Adopted = d.Adopted,
                PartnerId = d.PartnerId,
                HouseholdLevel = d.HouseholdLevel,
                Money = d.Money,
                MonthsInRed = d.MonthsInRed,
                Lease = d.Lease == null ? null : new Lease(d.Lease.TypeId, d.Lease.CommuneId, d.Lease.BaseRent, d.Lease.AdjustedPriceIndex, d.Lease.MonthsLeft, d.Lease.Deposit)
                {
                    MonthsSinceAdjustment = d.Lease.MonthsSinceAdjustment,
                },
                GroceryBasketId = d.GroceryBasketId,
                PensionFund = d.PensionFund,
                ContributedMonths = d.ContributedMonths,
                PensionSalaryBase = d.PensionSalaryBase,
                Pension = d.Pension,
                ReceivesUniversalPension = d.ReceivesUniversalPension,
                LastPensionPayment = d.LastPensionPayment,
                LastDebtPayment = d.LastDebtPayment,
                Delinquent = d.Delinquent,
                EducationStageId = d.EducationStageId,
                LastExpenses = ReadLines(d.LastExpenses),
            };

            if (d.CountryId != null && d.CityId != null && d.CommuneId != null)
            {
                character.Residence = new Residence(d.CountryId, d.CityId, d.CommuneId);
            }

            if (d.Job != null)
            {
                CareerPosition? position = d.Job.AreaId != null && d.Job.PositionId != null ? new CareerPosition(d.Job.AreaId, d.Job.PositionId) : null;
                var job = new Job(Parse<JobKind>(d.Job.Kind), d.Job.BaseSalary, d.Job.AdjustedPriceIndex, position);
                job.Restore(d.Job.AdjustedPriceIndex, d.Job.MonthsSinceAdjustment);
                job.MonthsInPosition = d.Job.MonthsInPosition;
                job.Performance = d.Job.Performance;
                job.CompanyId = d.Job.CompanyId;
                character.Job = job;
            }

            if (d.JobSearch != null)
            {
                character.JobSearch = new JobSearch(d.JobSearch.Target)
                {
                    Kind = Parse<JobKind>(d.JobSearch.Kind),
                    MethodId = d.JobSearch.MethodId,
                    ContactId = d.JobSearch.ContactId,
                    Stage = Parse<JobSearchStage>(d.JobSearch.Stage),
                    Weeks = d.JobSearch.Weeks,
                    Applications = d.JobSearch.Applications,
                    Interviews = d.JobSearch.Interviews,
                    Rejections = d.JobSearch.Rejections,
                    Negotiated = d.JobSearch.Negotiated,
                    Raise = d.JobSearch.Raise,
                };
            }

            if (d.Phone != null)
            {
                var phone = new Devices.Phone(Date(d.Phone.SinceWeek));
                foreach (string app in d.Phone.Apps)
                {
                    phone.Install(app);
                }

                character.Phone = phone;
            }

            if (d.Mail != null)
            {
                var mail = new Devices.MailAccount(Date(d.Mail.SinceWeek), d.Mail.JournalCursor);
                foreach (MailMessageData message in d.Mail.Messages)
                {
                    mail.Add(new Devices.MailMessage(Date(message.Week), message.MailId, message.Read));
                }

                character.Mail = mail;
            }

            if (d.LastAreaId != null && d.LastPositionId != null)
            {
                character.LastPosition = new CareerPosition(d.LastAreaId, d.LastPositionId);
            }

            if (d.LastPayslip != null)
            {
                character.LastPayslip = new Payslip(
                    Date(d.LastPayslip.Week),
                    Parse<JobKind>(d.LastPayslip.Kind),
                    d.LastPayslip.Gross,
                    ReadLines(d.LastPayslip.Lines),
                    d.LastPayslip.Taxable);
            }

            foreach (IncomeData i in d.Incomes)
            {
                character.RestoreIncome(new AnnualIncome(i.Year)
                {
                    EmployeeTaxable = i.EmployeeTaxable,
                    TaxWithheld = i.TaxWithheld,
                    FreelanceGross = i.FreelanceGross,
                    FreelanceRetention = i.FreelanceRetention,
                });
            }

            if (d.PendingTaxReturn != null)
            {
                TaxReturnData t = d.PendingTaxReturn;
                character.PendingTaxReturn = new TaxReturn(t.Year, t.TaxableIncome, t.Tax, t.Contributions, t.Credits);
            }

            foreach (DebtData debt in d.Debts)
            {
                character.AddDebt(new Debt(debt.TypeId, debt.Balance, debt.RemainingInstallments, debt.PriceIndex) { MonthsLate = debt.MonthsLate });
            }

            foreach (RelationshipData r in d.Relationships)
            {
                if (!Enum.TryParse(r.Bond, out RelationshipBond bond) || !Enum.IsDefined(typeof(RelationshipBond), bond))
                {
                    throw new SaveFormatException($"El guardado tiene un lazo que no existe: '{r.Bond}'.");
                }

                character.SetRelationship(new Relationship(r.OtherId, r.Affection, r.Trust, r.Respect, r.Resentment, bond));
            }

            foreach (string flag in d.Flags)
            {
                character.SetFlag(flag);
            }

            if (d.Studies != null)
            {
                StudiesData s = d.Studies;
                character.Studies = new CareerStudies(s.CareerId, s.LevelId, s.Institution, s.RequiredSemesters)
                {
                    CompletedSemesters = s.CompletedSemesters,
                    WeeksIntoSemester = s.WeeksIntoSemester,
                };
            }

            foreach (string degree in d.Degrees)
            {
                character.RestoreDegree(degree);
            }

            if (!d.IsAlive)
            {
                character.RestoreDeath(Date(d.DeathWeek ?? d.BirthWeek), d.DeathCauseId ?? Character.CauseNatural);
            }

            return character;
        }

        private static IReadOnlyList<PayslipLine> ReadLines(IEnumerable<MoneyLineData> lines) =>
            lines.Select(l => new PayslipLine(l.Id, l.Amount)).ToList();

        private static void ReadSeed(SeedBook book, SeedData s, ContentDatabase content)
        {
            var definition = new SeedDefinition(s.TypeId, Parse<SeedPolarity>(s.Polarity), s.Strength, s.ExpiresAfterWeeks)
            {
                MinWeeksDormant = s.MinWeeksDormant,
                Heritable = s.Heritable,

                // La condición para despertar no se guarda: sale del paquete, así un
                // arreglo en el paquete también llega a las partidas ya empezadas.
                Condition = content.HasSeedType(s.TypeId) ? content.GetSeedType(s.TypeId).ToSeedDefinition(content).Condition : null,
            };
            foreach (string eventId in s.PossibleEvents)
            {
                definition.PossibleEvents.Add(eventId);
            }

            Seed seed = book.Restore(s.Id, definition, s.OwnerId, Date(s.PlantedWeek), s.OwnerAgeAtPlanting, s.ParentSeedId);
            foreach (int witness in s.Witnesses)
            {
                seed.AddWitness(witness);
            }

            seed.Restore(
                Parse<SeedState>(s.State),
                s.AwakenedWeek.HasValue ? Date(s.AwakenedWeek.Value) : (GameDate?)null,
                s.ClosedWeek.HasValue ? Date(s.ClosedWeek.Value) : (GameDate?)null);
        }

        private static Value ReadLaw(LawChangeData law)
        {
            switch (law.Kind)
            {
                case "bool" when law.Bool.HasValue:
                    return Value.FromBool(law.Bool.Value);
                case "number" when law.Number.HasValue:
                    return Value.FromNumber(law.Number.Value);
                case "text" when law.Text != null:
                    return Value.FromText(law.Text);
                default:
                    throw new SaveFormatException($"La ley '{law.LawId}' guardada no tiene un valor válido.");
            }
        }

        private static GameDate Date(int week)
        {
            if (week < 0)
            {
                throw new SaveFormatException($"Fecha no válida en la partida guardada (semana {week}).");
            }

            return new GameDate(week);
        }

        private static T Parse<T>(string text)
            where T : struct
        {
            if (!Enum.TryParse(text, false, out T value) || !Enum.IsDefined(typeof(T), value))
            {
                throw new SaveFormatException($"Valor '{text}' no válido para {typeof(T).Name} en la partida guardada.");
            }

            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// La gente interesante de tu horario (D4b2, aprobado por Isaias el 20-11-2026): personas con
    /// algo real que darte, solo donde de verdad las verías.
    ///
    /// En tu pega, alguien con un cargo más alto que el tuyo en tu misma empresa (en tu área o en
    /// otra de tu carrera u oficio), solo si ese cargo existe en una empresa de ese tamaño. En la
    /// universidad, alguien que hace clases en tu carrera y trabaja en ella, solo si es una de las
    /// carreras que existen enteras en el juego. Nadie con cargos ni pegas inventadas.
    ///
    /// Aparecen de a poco (<see cref="MaxShown"/> a la vez como máximo) y mientras no conversan no
    /// hay relación de tu lado: no salen en tus conocidos. Para conocerlas hay que acercarse
    /// (una acción con <c>"con": "interesante"</c>): si te pesca, desde ahí es un conocido más y
    /// su ficha muestra lo que te puede aportar; si no te pesca <see cref="MaxDeclines"/> veces,
    /// se cierra la puerta. Si dejas esa pega o esa carrera, ya no la ves.
    /// </summary>
    public static class InterestingPeopleRules
    {
        /// <summary>La ves en tu pega.</summary>
        public const string WorkSource = "pega";

        /// <summary>La ves en tu carrera (hace clases).</summary>
        public const string StudiesSource = "estudios";

        /// <summary>Cuántas personas muestra la franja a la vez, como máximo.</summary>
        public const int MaxShown = 3;

        /// <summary>Cuántas de la pega, como máximo, a la vez.</summary>
        public const int MaxShownFromWork = 2;

        /// <summary>Cuántas de la carrera, como máximo, a la vez.</summary>
        public const int MaxShownFromStudies = 1;

        /// <summary>Cuántas veces te puede decir que no antes de cerrarte la puerta.</summary>
        public const int MaxDeclines = 2;

        /// <summary>Meses en la pega antes de fijarte en quién manda más arriba.</summary>
        public const int MonthsToSettle = 2;

        /// <summary>
        /// Cuántas personas de la pega aparecen en cada pega, en total: en una empresa te fijas en
        /// un par de personas que mandan, no en una nueva cada año.
        /// </summary>
        public const int MaxPerJob = 2;

        /// <summary>Semanas entre una persona nueva de la pega y la siguiente.</summary>
        public const int WeeksBetweenWork = 26;

        /// <summary>Semanas entre una persona nueva de la carrera y la siguiente.</summary>
        public const int WeeksBetweenStudies = 52;

        /// <summary>Probabilidad semanal de fijarte en alguien de tu carrera (en promedio, a los dos meses).</summary>
        public const double StudiesWeeklyChance = 0.125;

        /// <summary>Edades de quien hace clases en tu carrera.</summary>
        public const int MinTeacherAge = 32;

        /// <summary>Ver <see cref="MinTeacherAge"/>.</summary>
        public const int MaxTeacherAge = 60;

        /// <summary>Edad máxima de quien manda en tu empresa.</summary>
        public const int MaxManagerAge = 62;

        /// <summary>Lo que aparece en tu horario de esa persona, o null si nunca apareció.</summary>
        public static InterestingPerson? Entry(Character player, int personId) =>
            player.InterestingPeople.FirstOrDefault(p => p.PersonId == personId);

        /// <summary>Si ya conversaron: desde la primera conversación hay relación de tu lado.</summary>
        public static bool Met(Character player, int personId) => player.RelationshipWith(personId) != null;

        /// <summary>Si te cerró la puerta (te dijo que no <see cref="MaxDeclines"/> veces).</summary>
        public static bool Closed(InterestingPerson entry) => entry.Declines >= MaxDeclines;

        /// <summary>
        /// La gente que podrías conocer ahora: aparecieron en tu horario, siguen ahí (vivas, en tu
        /// empresa o en tu carrera), todavía no conversan y no te han cerrado la puerta.
        /// </summary>
        public static IReadOnlyList<Character> Shown(World world, ContentDatabase content, Character player)
        {
            var shown = new List<Character>();
            foreach (InterestingPerson entry in player.InterestingPeople)
            {
                if (!Closed(entry) && !Met(player, entry.PersonId) && world.TryGetCharacter(entry.PersonId, out Character? person)
                    && StillThere(world, content, player, entry, person))
                {
                    shown.Add(person);
                }
            }

            return shown.Take(MaxShown).ToList();
        }

        /// <summary>Si sigue en tu horario: viva y en tu empresa (la pega) o en tu carrera (la universidad).</summary>
        public static bool StillThere(World world, ContentDatabase content, Character player, InterestingPerson entry, Character person)
        {
            if (!person.IsAlive || !player.IsAlive)
            {
                return false;
            }

            if (entry.Source == WorkSource)
            {
                return Work.WorkRules.InTeam(player) && player.Job!.CompanyId.HasValue && person.Job?.CompanyId == player.Job.CompanyId;
            }

            return Education.UniversityRules.InSection(player) && person.Job?.Position != null
                && CareerOf(content, person) == player.Studies!.CareerId;
        }

        /// <summary>Te acercaste y te pescó: ya se conocen (desde ahora hay relación de tu lado).</summary>
        public static void Meet(World world, Character player, Character person)
        {
            RelationshipRules.Edit(world, player, person);
        }

        /// <summary>Cuánto más te ubica después de que te acercaste y no te pescó: la próxima vez ya te reconoce.</summary>
        public const int FamiliarityAfterDecline = 8;

        /// <summary>
        /// Te acercaste y no te pescó: una vez más cerca de que se cierre la puerta, aunque ya te
        /// ubica un poco más (<see cref="FamiliarityAfterDecline"/>) y la próxima vez puede ser distinto.
        /// </summary>
        public static void Decline(World world, Character player, Character person)
        {
            InterestingPerson? entry = Entry(player, person.Id);
            if (entry != null)
            {
                entry.Declines++;
                RelationshipRules.Edit(world, person, player).Affection += FamiliarityAfterDecline;
            }
        }

        /// <summary>
        /// Si manda en tu área: trabaja en tu misma empresa y en tu misma área, con un cargo más
        /// alto que el tuyo. Es quien te puede recomendar para un ascenso.
        /// </summary>
        public static bool ManagesYourArea(ContentDatabase content, Character player, Character person)
        {
            CareerPosition? mine = player.Job?.Position;
            CareerPosition? theirs = person.Job?.Position;
            if (mine == null || theirs == null || !player.Job!.CompanyId.HasValue || person.Job!.CompanyId != player.Job.CompanyId || theirs.AreaId != mine.AreaId)
            {
                return false;
            }

            return content.GetCareerPosition(theirs.PositionId).Depth > content.GetCareerPosition(mine.PositionId).Depth;
        }

        /// <summary>
        /// Si sabe de lo tuyo: trabaja en una pega del juego de la misma carrera u oficio en que
        /// trabajas o que estudias. Es quien te puede dar un consejo que sirve.
        /// </summary>
        public static bool KnowsYourTrade(ContentDatabase content, Character player, Character person)
        {
            string? theirs = CareerOf(content, person);
            if (theirs == null)
            {
                return false;
            }

            string? work = CareerOf(content, player);
            return theirs == work || theirs == player.Studies?.CareerId;
        }

        /// <summary>Cuántas personas interesantes ya conoce (conversaron al menos una vez).</summary>
        public static int MetCount(Character player) =>
            player.InterestingPeople.Count(p => Met(player, p.PersonId));

        /// <summary>
        /// Una semana: si lleva un tiempo en su pega o en su carrera, a veces aparece alguien
        /// nuevo que podría conocer (ver la explicación de la clase).
        /// </summary>
        public static void Week(World world, ContentDatabase content, Character player, GameRandom random)
        {
            if (!player.IsAlive || player.Residence == null)
            {
                return;
            }

            List<InterestingPerson> shown = player.InterestingPeople
                .Where(e => Shown(world, content, player).Any(p => p.Id == e.PersonId))
                .ToList();
            if (shown.Count >= MaxShown)
            {
                return;
            }

            Job? job = player.Job;
            if (Work.WorkRules.InTeam(player) && job!.Position != null && job.CompanyId.HasValue && job.MonthsInJob >= MonthsToSettle
                && shown.Count(e => e.Source == WorkSource) < MaxShownFromWork
                && FromThisJob(world, player, job) < MaxPerJob
                && SinceLast(world, player, WorkSource) >= Math.Min(WeeksBetweenWork, WeeksInJob(job)))
            {
                Character? person = Manager(world, content, player, random);
                if (person != null)
                {
                    player.InterestingPeople.Add(new InterestingPerson(person.Id, WorkSource, world.Date));
                    return;
                }
            }

            CareerStudies? studies = player.Studies;
            if (Education.UniversityRules.InSection(player) && shown.Count(e => e.Source == StudiesSource) < MaxShownFromStudies
                && SinceLast(world, player, StudiesSource) >= WeeksBetweenStudies && random.Chance(StudiesWeeklyChance))
            {
                CareerDefinition career = content.GetCareer(studies!.CareerId);
                Character? person = career.IsOccupation || career.Areas.Count == 0 ? null : Teacher(world, content, player, career, random);
                if (person != null)
                {
                    player.InterestingPeople.Add(new InterestingPerson(person.Id, StudiesSource, world.Date));
                }
            }
        }

        /// <summary>Semanas que lleva en su pega (contadas desde sus meses).</summary>
        private static int WeeksInJob(Job job) => job.MonthsInJob * GameDate.WeeksPerYear / 12;

        /// <summary>Cuántas personas de la pega aparecieron desde que entró a esta pega.</summary>
        private static int FromThisJob(World world, Character player, Job job)
        {
            int start = world.Date.TotalWeeks - WeeksInJob(job) - 5;
            return player.InterestingPeople.Count(e => e.Source == WorkSource && e.Since.TotalWeeks >= start);
        }

        /// <summary>Semanas desde que apareció la última persona de ese lugar (muchas si nunca).</summary>
        private static int SinceLast(World world, Character player, string source)
        {
            InterestingPerson? last = player.InterestingPeople.LastOrDefault(e => e.Source == source);
            return last == null ? int.MaxValue : world.Date.TotalWeeks - last.Since.TotalWeeks;
        }

        /// <summary>La carrera u oficio de la pega del juego de alguien, o null.</summary>
        private static string? CareerOf(ContentDatabase content, Character person) =>
            person.Job?.Position != null && content.TryGetCareerArea(person.Job.Position.AreaId, out CareerAreaDefinition? area) ? area!.CareerId : null;

        /// <summary>
        /// Alguien con un cargo más alto que el tuyo en tu empresa: la mitad de las veces de tu
        /// área, si no de cualquier área de tu carrera u oficio. Null si en tu empresa no hay
        /// cargos más altos que el tuyo (estás arriba, o es una empresa chica).
        /// </summary>
        private static Character? Manager(World world, ContentDatabase content, Character player, GameRandom random)
        {
            Job job = player.Job!;
            CareerAreaDefinition myArea = content.GetCareerArea(job.Position!.AreaId);
            CareerDefinition career = content.GetCareer(myArea.CareerId);
            int myDepth = content.GetCareerPosition(job.Position.PositionId).Depth;
            int? maxDepth = CompanyRules.MaxDepth(world, content, player);
            var above = career.Areas
                .SelectMany(a => a.Ladder.Select(p => (Area: a, Position: p)))
                .Where(x => x.Position.Depth > myDepth && (!maxDepth.HasValue || x.Position.Depth <= maxDepth.Value))
                .ToList();
            var mine = above.Where(x => x.Area.Id == myArea.Id).ToList();
            if (above.Count == 0)
            {
                return null;
            }

            (CareerAreaDefinition area, CareerPositionDefinition position) = mine.Count > 0 && random.Chance(0.5) ? random.Pick(mine) : random.Pick(above);
            int playerAge = player.AgeInYears(world.Date);
            int age = Math.Max(playerAge + 1, Math.Min(MaxManagerAge, 26 + (4 * position.Depth) + random.NextInt(-2, 9)));
            Sex sex = random.Chance(Work.WorkRules.FemaleBosses) ? Sex.Female : Sex.Male;
            Character person = NewPerson(world, content, player, sex, Math.Min(age, MaxManagerAge), random);
            Hire(world, content, person, career, area, position, world.CompanyById(job.CompanyId)!, random);
            return person;
        }

        /// <summary>Alguien que hace clases en tu carrera y trabaja en ella, con algunos años de experiencia.</summary>
        private static Character? Teacher(World world, ContentDatabase content, Character player, CareerDefinition career, GameRandom random)
        {
            CareerAreaDefinition area = random.Pick(career.Areas);
            List<CareerPositionDefinition> senior = area.Ladder.Where(p => p.Depth >= 1 && p.Depth <= 3).ToList();
            if (senior.Count == 0)
            {
                return null;
            }

            CompaniesDefinition? companies = CompanyRules.Definition(content, player);
            if (companies == null || companies.Sizes.Count == 0)
            {
                return null;
            }

            Character person = NewPerson(world, content, player, WorldFactory.RandomSex(random), random.NextInt(MinTeacherAge, MaxTeacherAge + 1), random);
            Company company = CompanyRules.Hire(world, content, person, career)!;

            CompanySizeDefinition? size = content.GetCountry(company.CountryId).Economy?.Companies?.Size(company.SizeId);
            CareerPositionDefinition position = CompanyRules.Cap(random.Pick(senior), size?.MaxPositionDepth);
            Hire(world, content, person, career, area, position, company, random);
            return person;
        }

        private static Character NewPerson(World world, ContentDatabase content, Character player, Sex sex, int age, GameRandom random)
        {
            CountryDefinition country = content.GetCountry(player.Residence!.CountryId);
            IReadOnlyList<string> pool = sex == Sex.Female ? country.Names.Female : country.Names.Male;
            var free = pool.Where(n => n != player.FirstName).ToList();
            string firstName = random.Pick(free.Count > 0 ? (IReadOnlyList<string>)free : pool);
            string lastName = WorldFactory.Surnames(country, random.Pick(country.Names.Surnames), random.Pick(country.Names.Surnames));
            Character person = WorldFactory.NewPerson(world, random, firstName, lastName, sex, WorldFactory.BirthDateForAge(world.Date, age, random));
            person.Residence = player.Residence;
            person.HouseholdLevel = Math.Max(0, Math.Min(100, player.HouseholdLevel + random.NextInt(5, 26)));
            Diversity.AssignAtBirth(person, country, random);
            world.AddCharacter(person);

            // Desde su lado ya te ubica un poco (de vista): eso decide si te pesca la primera vez.
            RelationshipRules.Edit(world, person, player).Affection = random.NextInt(Education.SchoolRules.MinFirstAffection, Education.SchoolRules.MaxFirstAffection + 1);
            return person;
        }

        /// <summary>Le da su pega del juego: su título (si la carrera lo pide), su cargo, su empresa y su sueldo.</summary>
        private static void Hire(World world, ContentDatabase content, Character person, CareerDefinition career, CareerAreaDefinition area, CareerPositionDefinition position, Company company, GameRandom random)
        {
            if (!career.IsOccupation && career.Levels.Count > 0)
            {
                person.AddDegree(random.Pick(career.Levels).Id);
            }

            var scope = new EventScope(world, content, person, null, readOnly: true);
            decimal salary = CareerLadder.Salary(scope.SalaryBasisOf(person, career), area, position);
            CompanySizeDefinition? size = content.GetCountry(company.CountryId).Economy?.Companies?.Size(company.SizeId);
            if (size != null)
            {
                double relative = career.IsOccupation ? Math.Max(1, size.SalaryRelative) : size.SalaryRelative;
                salary = Math.Round(salary * (decimal)relative, MidpointRounding.AwayFromZero);
            }

            double priceIndex = world.EconomyOf(person.Residence!.CountryId)?.PriceIndex ?? 1;
            Job job = CareerLadder.Place(person, JobKind.Employee, salary, priceIndex, area, position);
            job.CompanyId = company.Id;
            job.MonthsInPosition = random.NextInt(6, 60);
            person.Job = job;
            person.WorkStatus = WorkStatus.Working;
        }
    }
}

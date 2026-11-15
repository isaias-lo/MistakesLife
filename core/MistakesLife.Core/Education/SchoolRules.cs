using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Reglas del curso del colegio: quiénes son tus compañeros y tu profe jefe, cuándo
    /// hay clases y cómo se arma un curso. Valen para todos los países; cuántos alumnos
    /// hay y cuándo empiezan las clases vienen del paquete del país.
    /// </summary>
    public static class SchoolRules
    {
        /// <summary>Si el país no dice cómo es un curso: 35 alumnos y trato con 10.</summary>
        public static readonly SchoolClassDefinition DefaultClass = new SchoolClassDefinition { Students = 35, Named = 10 };

        /// <summary>Las letras de los cursos de un nivel (un colegio con dos o tres cursos por nivel).</summary>
        public static readonly IReadOnlyList<string> Letters = new[] { "A", "B", "C" };

        /// <summary>
        /// Cuántas semanas antes (en negativo) o después que tú nacen tus compañeros: en
        /// Chile entra al mismo curso quien cumple los años antes del 31 de marzo, así que hay
        /// compañeros hasta nueve meses mayores y unos tres meses menores.
        /// </summary>
        public const int OldestClassmateWeeks = -39;

        /// <summary>Ver <see cref="OldestClassmateWeeks"/>.</summary>
        public const int YoungestClassmateWeeks = 12;

        /// <summary>Edad mínima y máxima de quien hace clases.</summary>
        public const int MinTeacherAge = 25;

        /// <summary>Ver <see cref="MinTeacherAge"/>.</summary>
        public const int MaxTeacherAge = 62;

        /// <summary>Cariño con que empiezan dos compañeros nuevos (cada uno por el otro, al azar entre los dos).</summary>
        public const int MinFirstAffection = 20;

        /// <summary>Ver <see cref="MinFirstAffection"/>.</summary>
        public const int MaxFirstAffection = 50;

        /// <summary>Cariño que se tienen dos compañeros (los dos) para volverse amigos.</summary>
        public const int FriendshipAffection = 60;

        /// <summary>
        /// El año escolar de una fecha (cuenta desde el inicio de la partida y cambia el día
        /// que empiezan las clases); null si el país no dice cuándo empiezan.
        /// </summary>
        public static int? SchoolYearIndex(CountryDefinition country, GameDate date)
        {
            if (!country.SchoolYearStartDay.HasValue)
            {
                return null;
            }

            int startWeek = Math.Min(country.SchoolYearStartDay.Value / 7, GameDate.WeeksPerYear - 1);
            return (int)Math.Floor((date.TotalWeeks - startWeek) / (double)GameDate.WeeksPerYear);
        }

        /// <summary>La etapa del colegio con curso que le toca a esa persona hoy, o null.</summary>
        public static EducationStageDefinition? ClassStage(World world, ContentDatabase content, Character character)
        {
            if (character.Residence == null || character.Studies != null)
            {
                return null;
            }

            EducationStageDefinition? stage = content.GetCountry(character.Residence.CountryId).StageForAge(character.AgeInYears(world.Date));
            return stage != null && stage.HasClass ? stage : null;
        }

        /// <summary>Si hoy tiene clases: va a un curso y no son vacaciones.</summary>
        public static bool InClasses(World world, ContentDatabase content, Character character) =>
            character.SchoolClass != null && ClassStage(world, content, character) != null
            && content.GetCountry(character.Residence!.CountryId).IsSchoolTime(world.Date);

        /// <summary>Los compañeros con nombre que siguen vivos.</summary>
        public static IReadOnlyList<Character> Classmates(World world, Character character)
        {
            var people = new List<Character>();
            foreach (int id in character.SchoolClass?.ClassmateIds ?? new List<int>())
            {
                if (world.TryGetCharacter(id, out Character? person) && person.IsAlive)
                {
                    people.Add(person);
                }
            }

            return people;
        }

        /// <summary>La profe o el profe jefe, si hay y está vivo.</summary>
        public static Character? Teacher(World world, Character character) =>
            character.SchoolClass?.TeacherId is int id && world.TryGetCharacter(id, out Character? teacher) && teacher.IsAlive ? teacher : null;

        /// <summary>
        /// Arma el curso de una etapa nueva. Siguen contigo algunos de tu curso anterior y
        /// de tus amigos de tu edad (cada uno con la probabilidad del país); el resto son
        /// compañeros nuevos con nombre. Llega con su profe jefe.
        /// </summary>
        public static SchoolClass Form(World world, ContentDatabase content, Character player, EducationStageDefinition stage, int schoolYear, GameRandom random)
        {
            CountryDefinition country = content.GetCountry(player.Residence!.CountryId);
            SchoolClassDefinition rules = country.SchoolClass ?? DefaultClass;
            var cls = new SchoolClass(stage.Id, 1 + Math.Max(0, player.AgeInYears(world.Date) - stage.FromAge), random.Pick(Letters), schoolYear, rules.Students);

            // Los que ya conocías: tu curso de antes y tus amigos o rivales de tu edad.
            var known = Classmates(world, player).ToList();
            foreach (RelationshipBond bond in new[] { RelationshipBond.Friend, RelationshipBond.Rival })
            {
                known.AddRange(RelationshipRules.WithBond(world, player, bond)
                    .Where(p => Math.Abs(p.AgeInYears(world.Date) - player.AgeInYears(world.Date)) <= FamilyFactory.MaxSchoolAgeGap && p.AgeInYears(world.Date) < 18));
            }

            foreach (Character person in known.Distinct().OrderBy(p => p.Id))
            {
                if (cls.ClassmateIds.Count < rules.Named && random.Chance(rules.KeepOnNewStage))
                {
                    cls.ClassmateIds.Add(person.Id);
                }
            }

            while (cls.ClassmateIds.Count < rules.Named)
            {
                cls.ClassmateIds.Add(CreateClassmate(world, content, player, cls, random).Id);
            }

            cls.TeacherId = CreateTeacher(world, content, player, rules, random).Id;
            return cls;
        }

        /// <summary>
        /// Pasa de curso: un año más; a veces cambia la profe o el profe jefe, y a veces
        /// alguien se cambia de colegio y llega alguien nuevo. Devuelve las líneas del diario
        /// que corresponden.
        /// </summary>
        public static IReadOnlyList<string> Advance(World world, ContentDatabase content, Character player, int schoolYear, GameRandom random)
        {
            SchoolClass cls = player.SchoolClass!;
            SchoolClassDefinition rules = content.GetCountry(player.Residence!.CountryId).SchoolClass ?? DefaultClass;
            var journal = new List<string>();
            cls.Level++;
            cls.SchoolYear = schoolYear;

            if (Teacher(world, player) == null || random.Chance(rules.TeacherChange))
            {
                cls.TeacherId = CreateTeacher(world, content, player, rules, random).Id;
                journal.Add("diario.curso.profe_nueva");
            }

            cls.ClassmateIds.RemoveAll(id => !world.TryGetCharacter(id, out Character? c) || !c.IsAlive);
            if (cls.ClassmateIds.Count > 0 && random.Chance(rules.YearlyChange))
            {
                cls.ClassmateIds.RemoveAt(random.NextInt(cls.ClassmateIds.Count));
                journal.Add("diario.curso.se_cambio");
            }

            if (cls.ClassmateIds.Count < rules.Named)
            {
                while (cls.ClassmateIds.Count < rules.Named)
                {
                    cls.ClassmateIds.Add(CreateClassmate(world, content, player, cls, random).Id);
                }

                journal.Add("diario.curso.llego_nuevo");
            }

            return journal;
        }

        /// <summary>
        /// Los compañeros que se tienen cariño de verdad (los dos, sobre
        /// <see cref="FriendshipAffection"/>) y todavía no son amigos, pasan a serlo.
        /// Devuelve a los amigos nuevos.
        /// </summary>
        public static IReadOnlyList<Character> NewFriendships(World world, Character player)
        {
            var friends = new List<Character>();
            foreach (Character mate in Classmates(world, player))
            {
                Relationship mine = RelationshipRules.Feelings(world, player, mate);
                Relationship theirs = RelationshipRules.Feelings(world, mate, player);
                if (mine.Bond == RelationshipBond.None && mine.Affection >= FriendshipAffection && theirs.Affection >= FriendshipAffection)
                {
                    RelationshipRules.SetBond(world, player, mate, RelationshipBond.Friend);
                    friends.Add(mate);
                }
            }

            return friends;
        }

        /// <summary>
        /// Un compañero de tu curso sin lazo contigo, para que alguien que "conoces" en el
        /// colegio sea de tu curso y no un desconocido; null si no queda ninguno.
        /// </summary>
        public static Character? UnboundClassmate(World world, Character player, GameRandom random)
        {
            var free = Classmates(world, player).Where(c => RelationshipRules.Feelings(world, player, c).Bond == RelationshipBond.None).ToList();
            return free.Count == 0 ? null : free[random.NextInt(free.Count)];
        }

        private static Character CreateClassmate(World world, ContentDatabase content, Character player, SchoolClass cls, GameRandom random)
        {
            CountryDefinition country = content.GetCountry(player.Residence!.CountryId);
            Sex sex = WorldFactory.RandomSex(random);

            // Nombres distintos en el curso (y distinto al tuyo), para que no se confundan.
            var taken = new HashSet<string> { player.FirstName };
            foreach (int id in cls.ClassmateIds)
            {
                if (world.TryGetCharacter(id, out Character? other))
                {
                    taken.Add(other.FirstName);
                }
            }

            IReadOnlyList<string> pool = sex == Sex.Female ? country.Names.Female : country.Names.Male;
            var free = pool.Where(n => !taken.Contains(n)).ToList();
            string firstName = random.Pick(free.Count > 0 ? (IReadOnlyList<string>)free : pool);
            string lastName = WorldFactory.Surnames(country, random.Pick(country.Names.Surnames), random.Pick(country.Names.Surnames));
            var birth = new GameDate(player.BirthDate.TotalWeeks + random.NextInt(OldestClassmateWeeks, YoungestClassmateWeeks + 1));

            Character mate = WorldFactory.NewPerson(world, random, firstName, lastName, sex, birth);
            mate.Residence = player.Residence;
            mate.HouseholdLevel = Math.Max(0, Math.Min(100, player.HouseholdLevel + random.NextInt(-15, 16)));
            Diversity.AssignAtBirth(mate, country, random);
            world.AddCharacter(mate);

            // Desde el primer día hay compañeros que te caen mejor que otros (y a ellos, tú).
            player.SetRelationship(new Relationship(mate.Id, random.NextInt(MinFirstAffection, MaxFirstAffection + 1), 30, 30, 0));
            mate.SetRelationship(new Relationship(player.Id, random.NextInt(MinFirstAffection, MaxFirstAffection + 1), 30, 30, 0));
            return mate;
        }

        private static Character CreateTeacher(World world, ContentDatabase content, Character player, SchoolClassDefinition rules, GameRandom random)
        {
            CountryDefinition country = content.GetCountry(player.Residence!.CountryId);
            Sex sex = random.Chance(rules.FemaleTeachers) ? Sex.Female : Sex.Male;
            string firstName = random.Pick(sex == Sex.Female ? country.Names.Female : country.Names.Male);
            string lastName = WorldFactory.Surnames(country, random.Pick(country.Names.Surnames), random.Pick(country.Names.Surnames));
            int age = random.NextInt(MinTeacherAge, MaxTeacherAge + 1);

            Character teacher = WorldFactory.NewPerson(world, random, firstName, lastName, sex, WorldFactory.BirthDateForAge(world.Date, age, random));
            teacher.Residence = player.Residence;
            teacher.HouseholdLevel = 50;
            Diversity.AssignAtBirth(teacher, country, random);
            world.AddCharacter(teacher);
            return teacher;
        }
    }
}

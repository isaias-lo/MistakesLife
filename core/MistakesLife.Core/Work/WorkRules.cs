using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Education;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>Las reglas del equipo del trabajo (ver <see cref="WorkSystem"/>).</summary>
    public static class WorkRules
    {
        /// <summary>Colegas con nombre al llegar (además del jefe o la jefa).</summary>
        public const int TeamSize = 5;

        /// <summary>El equipo nunca baja de esta cantidad de colegas con nombre.</summary>
        public const int MinTeamSize = 3;

        /// <summary>Probabilidad de que un colega deje el equipo en un año (la rotación en Chile es alta).</summary>
        public const double LeavesPerYear = 0.2;

        /// <summary>Probabilidad de que llegue alguien nuevo por cada puesto que quedó vacío.</summary>
        public const double Replaced = 0.6;

        /// <summary>Probabilidad de que cambie el jefe o la jefa en un año.</summary>
        public const double BossChangesPerYear = 0.15;

        /// <summary>Edades de los colegas y del jefe.</summary>
        public const int MinColleagueAge = 20;

        /// <summary>Ver <see cref="MinColleagueAge"/>.</summary>
        public const int MaxColleagueAge = 60;

        /// <summary>Ver <see cref="MinColleagueAge"/>.</summary>
        public const int MinBossAge = 28;

        /// <summary>Ver <see cref="MinColleagueAge"/>.</summary>
        public const int MaxBossAge = 62;

        /// <summary>Proporción de jefas (en Chile, cerca de un tercio de las jefaturas son mujeres).</summary>
        public const double FemaleBosses = 0.33;

        /// <summary>Cariño que suma cada mes, a los dos, trabajar con alguien con quien ya viviste un momento.</summary>
        public const int SharedMonthAffection = 1;

        /// <summary>Si el jugador trabaja en un lugar con equipo: tiene trabajo, no a honorarios.</summary>
        public static bool HasTeamJob(Character character) =>
            character.Job != null && character.Job.Kind != JobKind.Freelance;

        /// <summary>Si hoy trabaja con su equipo armado.</summary>
        public static bool InTeam(Character character) => HasTeamJob(character) && character.Team != null;

        /// <summary>Los colegas del equipo que siguen vivos.</summary>
        public static IReadOnlyList<Character> Colleagues(World world, Character player)
        {
            WorkTeam? team = player.Team;
            if (team == null || !HasTeamJob(player))
            {
                return new List<Character>();
            }

            return team.ColleagueIds
                .Where(id => world.TryGetCharacter(id, out Character? c) && c.IsAlive)
                .Select(id => world.GetCharacter(id))
                .ToList();
        }

        /// <summary>El jefe o la jefa, si está vivo; null si no.</summary>
        public static Character? Boss(World world, Character player)
        {
            WorkTeam? team = player.Team;
            return team?.BossId is int id && HasTeamJob(player) && world.TryGetCharacter(id, out Character? boss) && boss.IsAlive ? boss : null;
        }

        /// <summary>Arma el equipo de un trabajo nuevo: jefe o jefa y <see cref="TeamSize"/> colegas, todos colegas tuyos desde hoy.</summary>
        public static WorkTeam Form(World world, ContentDatabase content, Character player, GameRandom random)
        {
            var team = new WorkTeam { MonthsInJob = player.Job!.MonthsInJob };
            team.BossId = NewPerson(world, content, player, team, random, boss: true).Id;
            while (team.ColleagueIds.Count < TeamSize)
            {
                team.ColleagueIds.Add(NewPerson(world, content, player, team, random, boss: false).Id);
            }

            return team;
        }

        /// <summary>
        /// Pasa un año en el trabajo: algunos colegas se van (los que no son tus amigos) y no
        /// siempre llega alguien en su lugar; a veces cambia el jefe. Devuelve las líneas del diario.
        /// </summary>
        public static IReadOnlyList<string> Year(World world, ContentDatabase content, Character player, GameRandom random)
        {
            WorkTeam team = player.Team!;
            var journal = new List<string>();
            team.ColleagueIds.RemoveAll(id => !world.TryGetCharacter(id, out Character? c) || !c.IsAlive);
            team.LeftIds.Clear();
            foreach (int id in team.ColleagueIds)
            {
                if (random.Chance(LeavesPerYear) && (player.RelationshipWith(id)?.Bond ?? RelationshipBond.None) != RelationshipBond.Friend)
                {
                    team.LeftIds.Add(id);
                }
            }

            team.ColleagueIds.RemoveAll(team.LeftIds.Contains);
            if (team.LeftIds.Count > 0)
            {
                journal.Add("diario.pega.se_fue");
            }

            int missing = TeamSize - team.ColleagueIds.Count;
            bool arrived = false;
            for (int i = 0; i < missing; i++)
            {
                if (team.ColleagueIds.Count < MinTeamSize || random.Chance(Replaced))
                {
                    team.ColleagueIds.Add(NewPerson(world, content, player, team, random, boss: false).Id);
                    arrived = true;
                }
            }

            if (arrived)
            {
                journal.Add("diario.pega.llego_alguien");
            }

            if (Boss(world, player) == null || random.Chance(BossChangesPerYear))
            {
                team.BossId = NewPerson(world, content, player, team, random, boss: true).Id;
                journal.Add("diario.pega.jefe_nuevo");
            }

            return journal;
        }

        /// <summary>
        /// En el trabajo ves todos los días a la gente con la que ya viviste algo: cada mes el
        /// cariño sube un poco, a los dos. Con el resto no pasa nada.
        /// </summary>
        public static void ShareMonth(World world, Character player)
        {
            IReadOnlyDictionary<int, int> shared = SchoolRules.SharedMoments(world, player);
            foreach (Character mate in Colleagues(world, player))
            {
                if (shared.ContainsKey(mate.Id))
                {
                    RelationshipRules.Edit(world, player, mate).Affection += SharedMonthAffection;
                    RelationshipRules.Edit(world, mate, player).Affection += SharedMonthAffection;
                }
            }
        }

        /// <summary>
        /// Los colegas que se quieren de verdad se vuelven amigos (como en el colegio: el otro te
        /// quiere y tú también, o han vivido juntos al menos <see cref="SchoolRules.FriendshipMoments"/> momentos).
        /// </summary>
        public static IReadOnlyList<Character> NewFriendships(World world, Character player)
        {
            var friends = new List<Character>();
            IReadOnlyDictionary<int, int> shared = SchoolRules.SharedMoments(world, player);
            foreach (Character mate in Colleagues(world, player))
            {
                Relationship mine = RelationshipRules.Feelings(world, player, mate);
                Relationship theirs = RelationshipRules.Feelings(world, mate, player);
                bool yours = mine.Affection >= SchoolRules.FriendshipAffection || (shared.TryGetValue(mate.Id, out int moments) && moments >= SchoolRules.FriendshipMoments);
                if (mine.Bond == RelationshipBond.Colleague && yours && theirs.Affection >= SchoolRules.FriendshipAffection)
                {
                    RelationshipRules.SetBond(world, player, mate, RelationshipBond.Friend);
                    friends.Add(mate);
                }
            }

            return friends;
        }

        private static Character NewPerson(World world, ContentDatabase content, Character player, WorkTeam team, GameRandom random, bool boss)
        {
            CountryDefinition country = content.GetCountry(player.Residence!.CountryId);
            Sex sex = boss ? (random.Chance(FemaleBosses) ? Sex.Female : Sex.Male) : WorldFactory.RandomSex(random);

            // Nombres distintos en el equipo (y distinto al tuyo), para que no se confundan.
            var taken = new HashSet<string> { player.FirstName };
            foreach (int id in team.ColleagueIds.Concat(team.BossId.HasValue ? new[] { team.BossId.Value } : new int[0]))
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
            int playerAge = player.AgeInYears(world.Date);
            int age = boss
                ? random.NextInt(Math.Max(MinBossAge, Math.Min(playerAge + 2, MaxBossAge - 10)), MaxBossAge + 1)
                : Math.Max(MinColleagueAge, Math.Min(MaxColleagueAge, playerAge + random.NextInt(-8, 13)));

            Character person = WorldFactory.NewPerson(world, random, firstName, lastName, sex, WorldFactory.BirthDateForAge(world.Date, age, random));
            person.Residence = player.Residence;
            person.HouseholdLevel = Math.Max(0, Math.Min(100, player.HouseholdLevel + random.NextInt(-15, 16)));
            Diversity.AssignAtBirth(person, country, random);
            world.AddCharacter(person);

            // Desde el primer día hay colegas que te caen mejor que otros (y a ellos, tú).
            RelationshipRules.SetBond(world, player, person, RelationshipBond.Colleague);
            RelationshipRules.Edit(world, player, person).Affection = random.NextInt(SchoolRules.MinFirstAffection, SchoolRules.MaxFirstAffection + 1);
            RelationshipRules.Edit(world, person, player).Affection = random.NextInt(SchoolRules.MinFirstAffection, SchoolRules.MaxFirstAffection + 1);
            return person;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Worlds
{
    /// <summary>Crea un mundo nuevo con un recién nacido como jugador.</summary>
    public static class WorldFactory
    {
        /// <summary>
        /// Años de "pasado" antes del nacimiento del jugador, para que existan
        /// sus padres y, más adelante, sus abuelos e historia del país.
        /// </summary>
        public const int HistoryYears = 80;

        /// <summary>
        /// Vida mínima, sin país ni familia (útil para pruebas del núcleo).
        /// </summary>
        public static World CreateNewLife(ulong seed)
        {
            var world = new World(seed);
            GameRandom random = world.Random.Fork("mundo.nacimiento");
            Character player = NewPerson(world, random, "nombre.pendiente", "apellido.pendiente", RandomSex(random), world.Date);
            world.AddCharacter(player);
            world.SetPlayer(player.Id);
            world.WriteJournal(player.Id, "diario.nacimiento");
            return world;
        }

        /// <summary>
        /// Vida completa: país, ciudad y comuna al azar según los paquetes,
        /// con madre y padre, nombres del país y el nivel del hogar.
        /// </summary>
        public static World CreateNewLife(ulong seed, ContentDatabase content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            List<CountryDefinition> countries = content.Countries.ToList();
            if (countries.Count == 0)
            {
                throw new InvalidOperationException("No hay ningún paquete de país cargado.");
            }

            var world = new World(seed, new GameDate(HistoryYears * GameDate.WeeksPerYear));
            GameRandom random = world.Random.Fork("mundo.nacimiento");

            // Dónde nace.
            CountryDefinition country = countries[random.PickWeightedIndex(countries.Select(c => c.BirthWeight).ToList())];
            CityDefinition city = country.Cities[random.PickWeightedIndex(country.Cities.Select(c => c.BirthWeight).ToList())];
            CommuneDefinition commune = city.Communes[random.PickWeightedIndex(city.Communes.Select(c => c.BirthWeight).ToList())];
            var residence = new Residence(country.Id, city.Id, commune.Id);
            int householdLevel = random.NextInt(commune.HouseholdLevelMin, commune.HouseholdLevelMax + 1);

            // Madre y padre.
            int motherAge = (random.NextInt(country.MotherAgeMin, country.MotherAgeMax + 1) + random.NextInt(country.MotherAgeMin, country.MotherAgeMax + 1)) / 2;
            int fatherAge = Math.Max(country.MotherAgeMin, motherAge + random.NextInt(-3, 8));
            string fatherSurname = random.Pick(country.Names.Surnames);
            string motherSurname = random.Pick(country.Names.Surnames);
            string motherSecond = random.Pick(country.Names.Surnames);
            string fatherSecond = random.Pick(country.Names.Surnames);

            Character mother = NewPerson(world, random, random.Pick(country.Names.Female), Surnames(country, motherSurname, motherSecond), Sex.Female, BirthDateForAge(world.Date, motherAge, random));
            Character father = NewPerson(world, random, random.Pick(country.Names.Male), Surnames(country, fatherSurname, fatherSecond), Sex.Male, BirthDateForAge(world.Date, fatherAge, random));
            foreach (Character parent in new[] { mother, father })
            {
                parent.Residence = residence;
                parent.HouseholdLevel = householdLevel;
                world.AddCharacter(parent);
            }

            // El jugador.
            Sex sex = RandomSex(random);
            string firstName = random.Pick(sex == Sex.Female ? country.Names.Female : country.Names.Male);
            Character player = NewPerson(world, random, firstName, Surnames(country, fatherSurname, motherSurname), sex, world.Date);
            player.MotherId = mother.Id;
            player.FatherId = father.Id;
            player.Residence = residence;
            player.HouseholdLevel = householdLevel;
            world.AddCharacter(player);
            world.SetPlayer(player.Id);
            world.WriteJournal(player.Id, "diario.nacimiento");
            return world;
        }

        private static string Surnames(CountryDefinition country, string first, string second) =>
            country.TwoSurnames ? first + " " + second : first;

        private static GameDate BirthDateForAge(GameDate today, int ageInYears, GameRandom random) =>
            new GameDate(today.TotalWeeks - (ageInYears * GameDate.WeeksPerYear) - random.NextInt(GameDate.WeeksPerYear));

        private static Sex RandomSex(GameRandom random) => random.Chance(0.5) ? Sex.Female : Sex.Male;

        private static Character NewPerson(World world, GameRandom random, string firstName, string lastName, Sex sex, GameDate birthDate) =>
            new Character(
                world.NextCharacterId(),
                firstName,
                lastName,
                sex,
                birthDate,
                Personality.Random(random),
                new Stats(
                    health: random.NextInt(70, 101),
                    happiness: random.NextInt(60, 101),
                    smarts: random.NextInt(30, 71)));
    }
}

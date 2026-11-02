using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Crea a las personas nuevas que llegan a la vida de un personaje: su pareja
    /// y sus hijos. Los nombres salen del paquete de su país. Todo el azar sale
    /// del generador de la partida, así que la misma vida da la misma familia.
    /// </summary>
    public static class FamilyFactory
    {
        /// <summary>Edad mínima de una pareja creada por el juego.</summary>
        public const int MinPartnerAge = 18;

        /// <summary>
        /// Crea una pareja para <paramref name="person"/>, de edad parecida y del
        /// mismo lugar, y los une. El género de la pareja sale de la orientación
        /// de la persona (ver <see cref="Diversity.PartnerGender"/>), salvo que un
        /// evento lo pida con <paramref name="gender"/>. La pareja es cis y su
        /// orientación calza con la persona.
        /// </summary>
        public static Character CreatePartner(World world, ContentDatabase content, Character person, Gender? gender = null)
        {
            Check(world, content, person);
            if (person.PartnerId.HasValue)
            {
                throw new InvalidOperationException($"{person} ya tiene pareja.");
            }

            CountryDefinition country = content.GetCountry(person.Residence!.CountryId);
            GameRandom random = RandomFor(world, person, "pareja");
            Gender partnerGender = gender ?? Diversity.PartnerGender(person, random);
            Sex sex = partnerGender == Gender.Female ? Sex.Female
                : partnerGender == Gender.Male ? Sex.Male
                : WorldFactory.RandomSex(random.Fork("pareja.sexo"));
            int age = Math.Max(MinPartnerAge, person.AgeInYears(world.Date) + random.NextInt(-4, 5));
            string firstName = random.Pick(sex == Sex.Female ? country.Names.Female : country.Names.Male);
            string lastName = WorldFactory.Surnames(country, random.Pick(country.Names.Surnames), random.Pick(country.Names.Surnames));

            Character partner = WorldFactory.NewPerson(world, random, firstName, lastName, sex, WorldFactory.BirthDateForAge(world.Date, age, random));
            partner.Residence = person.Residence;
            partner.HouseholdLevel = person.HouseholdLevel;
            partner.Gender = partnerGender;
            partner.GenderIdentity = partnerGender;
            Diversity.AssignToPartner(partner, person, country, random);
            world.AddCharacter(partner);

            person.PartnerId = partner.Id;
            partner.PartnerId = person.Id;
            return partner;
        }

        /// <summary>Termina la pareja de <paramref name="person"/>. La otra persona sigue en el mundo.</summary>
        public static void EndPartnership(World world, Character person)
        {
            if (!person.PartnerId.HasValue)
            {
                throw new InvalidOperationException($"{person} no tiene pareja.");
            }

            if (world.TryGetCharacter(person.PartnerId.Value, out Character? partner) && partner.PartnerId == person.Id)
            {
                partner.PartnerId = null;
            }

            person.PartnerId = null;
        }

        /// <summary>
        /// Nace un hijo de <paramref name="parent"/> y de su pareja (si tiene).
        /// Lleva el primer apellido del padre y luego el de la madre; con un solo
        /// padre o madre, sus dos apellidos. Su personalidad es mitad herencia y
        /// mitad propia (ver <see cref="Personality.Inherit"/>).
        /// </summary>
        public static Character CreateChild(World world, ContentDatabase content, Character parent)
        {
            Check(world, content, parent);
            Character? other = null;
            if (parent.PartnerId.HasValue && world.TryGetCharacter(parent.PartnerId.Value, out Character? partner) && partner.IsAlive)
            {
                other = partner;
            }

            Character? mother = parent.Sex == Sex.Female ? parent : other?.Sex == Sex.Female ? other : null;
            Character? father = parent.Sex == Sex.Male ? parent : other?.Sex == Sex.Male ? other : null;

            CountryDefinition country = content.GetCountry(parent.Residence!.CountryId);
            GameRandom random = RandomFor(world, parent, "hijo");
            Sex sex = WorldFactory.RandomSex(random);
            string firstName = random.Pick(sex == Sex.Female ? country.Names.Female : country.Names.Male);
            string lastName = ChildSurnames(country, father, mother, parent);
            Personality personality = Personality.Inherit(parent.Personality, other?.Personality, random);

            Character child = WorldFactory.NewPerson(world, random, firstName, lastName, sex, world.Date, personality);
            child.MotherId = mother?.Id;
            child.FatherId = father?.Id;
            child.Residence = parent.Residence;
            child.HouseholdLevel = parent.HouseholdLevel;
            Diversity.AssignAtBirth(child, country, random);
            world.AddCharacter(child);
            return child;
        }

        /// <summary>
        /// Elige un nombre de pila nuevo del país, acorde al género con que vive
        /// (para no binario, de cualquiera de las dos listas). Se usa, por
        /// ejemplo, al cambiar el nombre registral.
        /// </summary>
        public static void ChangeFirstName(World world, ContentDatabase content, Character person)
        {
            Check(world, content, person);
            CountryDefinition country = content.GetCountry(person.Residence!.CountryId);
            GameRandom random = RandomFor(world, person, "nombre");
            bool female = person.Gender == Gender.Female || (person.Gender == Gender.NonBinary && random.Chance(0.5));
            IReadOnlyList<string> names = (female ? country.Names.Female : country.Names.Male).Where(n => n != person.FirstName).ToList();
            person.ChangeFirstName(random.Pick(names));
        }

        private static string ChildSurnames(CountryDefinition country, Character? father, Character? mother, Character parent)
        {
            if (father != null && mother != null)
            {
                return WorldFactory.Surnames(country, FirstSurname(father), FirstSurname(mother));
            }

            // Un solo padre o madre conocido: el hijo lleva sus apellidos.
            return parent.LastName;
        }

        private static string FirstSurname(Character person) => person.LastName.Split(' ')[0];

        private static void Check(World world, ContentDatabase content, Character person)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (person.Residence == null)
            {
                throw new InvalidOperationException($"{person} no vive en ningún país: no se puede crear su familia.");
            }
        }

        /// <summary>Azar propio para cada persona nueva: depende de la partida, la fecha y quién es.</summary>
        private static GameRandom RandomFor(World world, Character person, string what) =>
            world.Random.Fork($"familia.{what}.{person.Id}.{world.Date.TotalWeeks}.{world.Characters.Count}");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Crea a las personas nuevas que llegan a la vida de un personaje: su pareja,
    /// sus hijos, sus amigos, colegas y rivales. Los nombres salen del paquete de su país. Todo el azar sale
    /// del generador de la partida, así que la misma vida da la misma familia.
    /// </summary>
    public static class FamilyFactory
    {
        /// <summary>Edad mínima de una pareja creada por el juego.</summary>
        public const int MinPartnerAge = 18;

        /// <summary>Edad máxima del niño o niña que llega en adopción (en Chile, casi siempre pequeños).</summary>
        public const int MaxAdoptedChildAge = 5;

        /// <summary>Edad máxima con que una persona no jugada adopta.</summary>
        public const int MaxAdoptionAge = 50;

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

        /// <summary>
        /// Termina la pareja de <paramref name="person"/>. La otra persona sigue en
        /// el mundo. Si se separan (<paramref name="breakUp"/>), quedan como ex y
        /// guardan lo que sentían; si termina porque uno murió, no.
        /// </summary>
        public static void EndPartnership(World world, Character person, bool breakUp = true)
        {
            if (!person.PartnerId.HasValue)
            {
                throw new InvalidOperationException($"{person} no tiene pareja.");
            }

            if (breakUp && world.TryGetCharacter(person.PartnerId.Value, out Character? partner) && partner.PartnerId == person.Id)
            {
                // Lo que sentían como pareja se guarda antes de separarse: queda como ex.
                RelationshipRules.Edit(world, person, partner);
                RelationshipRules.Edit(world, partner, person);
                partner.PartnerId = null;
                person.PartnerId = null;
                RelationshipRules.SetBond(world, person, partner, RelationshipBond.Ex);
                return;
            }

            if (world.TryGetCharacter(person.PartnerId.Value, out Character? other) && other.PartnerId == person.Id)
            {
                other.PartnerId = null;
            }

            person.PartnerId = null;
        }

        /// <summary>Diferencia máxima de edad (en años) con alguien que se conoce en el colegio.</summary>
        public const int MaxSchoolAgeGap = 1;

        /// <summary>Diferencia máxima de edad (en años) con alguien que se conoce de adulto.</summary>
        public const int MaxAdultAgeGap = 8;

        /// <summary>
        /// Llega a la vida de <paramref name="person"/> alguien nuevo: un amigo, un
        /// colega o un rival, del mismo lugar y de edad parecida (casi la misma en
        /// el colegio; hasta <see cref="MaxAdultAgeGap"/> años de diferencia de
        /// adulto). Queda con ese lazo en ambos sentidos.
        /// </summary>
        public static Character CreateAcquaintance(World world, ContentDatabase content, Character person, RelationshipBond bond)
        {
            Check(world, content, person);
            if (bond != RelationshipBond.Friend && bond != RelationshipBond.Colleague && bond != RelationshipBond.Rival)
            {
                throw new ArgumentOutOfRangeException(nameof(bond), bond, "Solo se conoce a un amigo, un colega o un rival.");
            }

            CountryDefinition country = content.GetCountry(person.Residence!.CountryId);
            GameRandom random = RandomFor(world, person, "conocido");
            Sex sex = WorldFactory.RandomSex(random);
            int personAge = person.AgeInYears(world.Date);
            int gap = personAge < 18 ? MaxSchoolAgeGap : MaxAdultAgeGap;
            int age = Math.Max(0, personAge + random.NextInt(-gap, gap + 1));
            if (personAge >= 18)
            {
                age = Math.Max(18, age);
            }

            string firstName = random.Pick(sex == Sex.Female ? country.Names.Female : country.Names.Male);
            string lastName = WorldFactory.Surnames(country, random.Pick(country.Names.Surnames), random.Pick(country.Names.Surnames));
            Character other = WorldFactory.NewPerson(world, random, firstName, lastName, sex, WorldFactory.BirthDateForAge(world.Date, age, random));
            other.Residence = person.Residence;
            other.HouseholdLevel = Math.Max(0, Math.Min(100, person.HouseholdLevel + random.NextInt(-15, 16)));
            Diversity.AssignAtBirth(other, country, random);
            world.AddCharacter(other);

            RelationshipRules.SetBond(world, person, other, bond);
            return other;
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
        /// Llega por adopción un niño o niña (de 0 a <see cref="MaxAdoptedChildAge"/>
        /// años) a la familia de <paramref name="parent"/> y su pareja, si tiene.
        /// Sus padres biológicos no están en el mundo y su personalidad es propia.
        /// Lleva el primer apellido de quien adopta y luego el de su pareja.
        /// </summary>
        public static Character Adopt(World world, ContentDatabase content, Character parent)
        {
            Check(world, content, parent);
            Character? other = null;
            if (parent.PartnerId.HasValue && world.TryGetCharacter(parent.PartnerId.Value, out Character? partner) && partner.IsAlive)
            {
                other = partner;
            }

            CountryDefinition country = content.GetCountry(parent.Residence!.CountryId);
            GameRandom random = RandomFor(world, parent, "adopcion");
            Sex sex = WorldFactory.RandomSex(random);
            string firstName = random.Pick(sex == Sex.Female ? country.Names.Female : country.Names.Male);
            string lastName = other == null ? parent.LastName : WorldFactory.Surnames(country, FirstSurname(parent), FirstSurname(other));
            GameDate birth = WorldFactory.BirthDateForAge(world.Date, random.NextInt(0, MaxAdoptedChildAge + 1), random);

            Character child = WorldFactory.NewPerson(world, random, firstName, lastName, sex, birth);
            child.Adopted = true;
            foreach (Character adoptive in other == null ? new[] { parent } : new[] { parent, other })
            {
                AddParent(child, adoptive);
            }

            child.Residence = parent.Residence;
            child.HouseholdLevel = parent.HouseholdLevel;
            Diversity.AssignAtBirth(child, country, random);
            world.AddCharacter(child);
            return child;
        }

        /// <summary>Pone a la persona como madre o padre según su género; si ese lugar ya está, como segunda madre o padre.</summary>
        private static void AddParent(Character child, Character parent)
        {
            if (parent.Gender == Gender.Female && !child.MotherId.HasValue)
            {
                child.MotherId = parent.Id;
            }
            else if (parent.Gender == Gender.Male && !child.FatherId.HasValue)
            {
                child.FatherId = parent.Id;
            }
            else if (parent.Gender == Gender.NonBinary && !child.MotherId.HasValue && !child.FatherId.HasValue)
            {
                child.FatherId = parent.Id;
            }
            else
            {
                child.SecondParentId = parent.Id;
            }
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

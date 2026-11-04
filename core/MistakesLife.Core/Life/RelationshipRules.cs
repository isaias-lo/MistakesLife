using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Reglas de las relaciones: con qué sentimientos empieza cada lazo, cómo se
    /// leen y cambian, y quiénes son los amigos, colegas, rivales y ex de alguien.
    /// Valen para todos los países: lo que hace cambiar los sentimientos (una
    /// pelea, un préstamo, un olvido) va en los eventos de los paquetes.
    /// </summary>
    public static class RelationshipRules
    {
        /// <summary>Lo que sienten entre sí madres o padres e hijos al empezar.</summary>
        public static readonly (int Affection, int Trust, int Respect, int Resentment) ParentChild = (75, 65, 55, 0);

        /// <summary>Lo que sienten entre sí hermanos (comparten madre o padre) al empezar.</summary>
        public static readonly (int Affection, int Trust, int Respect, int Resentment) Siblings = (60, 55, 45, 0);

        /// <summary>Lo que siente una pareja al empezar.</summary>
        public static readonly (int Affection, int Trust, int Respect, int Resentment) Partners = (75, 60, 55, 0);

        /// <summary>Lo que siente alguien por una persona con quien no tiene un lazo.</summary>
        public static readonly (int Affection, int Trust, int Respect, int Resentment) Strangers = (30, 30, 30, 0);

        /// <summary>Con qué sentimientos empieza un lazo nuevo (al conocerse o al cambiar de lazo).</summary>
        public static (int Affection, int Trust, int Respect, int Resentment) Initial(RelationshipBond bond)
        {
            switch (bond)
            {
                case RelationshipBond.Friend:
                    return (55, 50, 45, 0);
                case RelationshipBond.Colleague:
                    return (35, 40, 50, 0);
                case RelationshipBond.Rival:
                    return (10, 15, 35, 45);
                default:
                    return Strangers;
            }
        }

        /// <summary>
        /// Lo que <paramref name="who"/> siente por <paramref name="toward"/>: lo
        /// guardado si ya pasó algo entre ellos, o si no, lo que corresponde a su
        /// lazo familiar (sin guardarlo). Para cambiarlo, usa <see cref="Edit"/>.
        /// </summary>
        public static Relationship Feelings(World world, Character who, Character toward)
        {
            return who.RelationshipWith(toward.Id) ?? Default(world, who, toward);
        }

        /// <summary>Lo que <paramref name="who"/> siente por <paramref name="toward"/>, guardado para poder cambiarlo.</summary>
        public static Relationship Edit(World world, Character who, Character toward)
        {
            Relationship? stored = who.RelationshipWith(toward.Id);
            if (stored != null)
            {
                return stored;
            }

            Relationship created = Default(world, who, toward);
            who.SetRelationship(created);
            return created;
        }

        /// <summary>
        /// Pone el lazo entre dos personas, en ambos sentidos. Si el lazo es nuevo
        /// para alguno, sus sentimientos empiezan como dice <see cref="Initial"/>
        /// solo si todavía no había pasado nada entre ellos; si ya se conocían,
        /// lo que sienten se mantiene.
        /// </summary>
        public static void SetBond(World world, Character a, Character b, RelationshipBond bond)
        {
            if (a.Id == b.Id)
            {
                throw new InvalidOperationException($"{a} no puede tener un lazo consigo mismo.");
            }

            SetOneWay(world, a, b, bond);
            SetOneWay(world, b, a, bond);
        }

        private static void SetOneWay(World world, Character who, Character toward, RelationshipBond bond)
        {
            Relationship? stored = who.RelationshipWith(toward.Id);
            if (stored == null)
            {
                var start = IsFamily(world, who, toward) || bond == RelationshipBond.None || bond == RelationshipBond.Ex
                    ? Values(Default(world, who, toward))
                    : Initial(bond);
                stored = new Relationship(toward.Id, start.Affection, start.Trust, start.Respect, start.Resentment);
                who.SetRelationship(stored);
            }

            stored.Bond = bond;
        }

        /// <summary>Las personas vivas que tienen ese lazo con <paramref name="person"/>, por id.</summary>
        public static IReadOnlyList<Character> WithBond(World world, Character person, RelationshipBond bond)
        {
            var people = new List<Character>();
            foreach (Relationship relationship in person.Relationships)
            {
                if (relationship.Bond == bond && world.TryGetCharacter(relationship.OtherId, out Character? other) && other.IsAlive)
                {
                    people.Add(other);
                }
            }

            return people;
        }

        /// <summary>
        /// La persona más cercana con ese lazo, según lo que ella siente por
        /// <paramref name="person"/>: el amigo con más cariño, el colega con más
        /// respeto, el rival con más rencor, el ex con más cariño. Null si no hay.
        /// </summary>
        public static Character? Closest(World world, Character person, RelationshipBond bond)
        {
            Character? best = null;
            int bestScore = int.MinValue;
            foreach (Character other in WithBond(world, person, bond))
            {
                Relationship feelings = Feelings(world, other, person);
                int score = bond == RelationshipBond.Colleague ? feelings.Respect
                    : bond == RelationshipBond.Rival ? feelings.Resentment
                    : feelings.Affection;
                if (score > bestScore)
                {
                    best = other;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>Clave del lazo en el lenguaje: 'amistad', 'colega', 'rival', 'ex', o null si no hay lazo.</summary>
        public static string? BondKey(RelationshipBond bond)
        {
            switch (bond)
            {
                case RelationshipBond.Friend:
                    return "amistad";
                case RelationshipBond.Colleague:
                    return "colega";
                case RelationshipBond.Rival:
                    return "rival";
                case RelationshipBond.Ex:
                    return "ex";
                default:
                    return null;
            }
        }

        /// <summary>Lee la clave de un lazo que se puede crear con un evento: 'amistad', 'colega' o 'rival'.</summary>
        public static bool TryParseBond(string key, out RelationshipBond bond)
        {
            switch (key)
            {
                case "amistad":
                    bond = RelationshipBond.Friend;
                    return true;
                case "colega":
                    bond = RelationshipBond.Colleague;
                    return true;
                case "rival":
                    bond = RelationshipBond.Rival;
                    return true;
                default:
                    bond = RelationshipBond.None;
                    return false;
            }
        }

        /// <summary>Los lazos que un evento puede crear, como se escriben.</summary>
        public static readonly IReadOnlyCollection<string> BondKeys = new[] { "amistad", "colega", "rival" };

        /// <summary>Si son familia directa: madre o padre e hijo, o hermanos.</summary>
        public static bool IsFamily(World world, Character a, Character b) =>
            a.IsChildOf(b.Id) || b.IsChildOf(a.Id) || AreSiblings(a, b);

        private static bool AreSiblings(Character a, Character b)
        {
            int?[] parentsA = { a.MotherId, a.FatherId, a.SecondParentId };
            int?[] parentsB = { b.MotherId, b.FatherId, b.SecondParentId };
            return parentsA.Any(p => p.HasValue && parentsB.Contains(p));
        }

        private static Relationship Default(World world, Character who, Character toward)
        {
            var start = who.IsChildOf(toward.Id) || toward.IsChildOf(who.Id) ? ParentChild
                : who.PartnerId == toward.Id ? Partners
                : AreSiblings(who, toward) ? Siblings
                : Strangers;
            return new Relationship(toward.Id, start.Affection, start.Trust, start.Respect, start.Resentment);
        }

        private static (int Affection, int Trust, int Respect, int Resentment) Values(Relationship r) =>
            (r.Affection, r.Trust, r.Respect, r.Resentment);
    }
}

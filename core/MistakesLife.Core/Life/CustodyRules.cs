using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// El cuidado personal de los hijos menores cuando sus papás ya no son pareja: con
    /// quién vive cada hijo. Mientras sus papás siguen juntos, o si no se ha decidido,
    /// el hijo vive en la casa de los dos. Lo deciden los eventos de los paquetes (un
    /// acuerdo, una mediación, una sentencia del juzgado) con
    /// <see cref="KeepChildren"/>, <see cref="GiveChildren"/> o <see cref="ShareCustody"/>.
    /// Si muere el papá con el que vivía, vuelve a vivir con el otro.
    /// </summary>
    public static class CustodyRules
    {
        /// <summary>Hasta qué edad un hijo es menor de edad (no elige solo con quién vivir).</summary>
        public const int AdultAge = 18;

        /// <summary>Hijos vivos y menores de edad de una persona, del mayor al menor.</summary>
        public static IReadOnlyList<Character> MinorChildren(World world, Character parent) =>
            world.ChildrenOf(parent.Id).Where(c => c.IsAlive && c.AgeInYears(world.Date) < AdultAge).ToList();

        /// <summary>El otro papá (o la otra mamá) del hijo, si existe en el mundo.</summary>
        public static Character? OtherParent(World world, Character child, Character parent)
        {
            foreach (int? id in new[] { child.MotherId, child.FatherId, child.SecondParentId })
            {
                if (id.HasValue && id.Value != parent.Id && world.TryGetCharacter(id.Value, out Character? other))
                {
                    return other;
                }
            }

            return null;
        }

        /// <summary>
        /// Si sus papás están separados: el otro papá vive y no es la pareja actual de
        /// <paramref name="parent"/>.
        /// </summary>
        public static bool ParentsApart(World world, Character child, Character parent)
        {
            Character? other = OtherParent(world, child, parent);
            return other != null && other.IsAlive && parent.PartnerId != other.Id;
        }

        /// <summary>Si el hijo vive en la casa de <paramref name="parent"/>.</summary>
        public static bool LivesWith(World world, Character child, Character parent)
        {
            if (!ParentsApart(world, child, parent) || child.SharedCustody || !child.LivesWithId.HasValue)
            {
                return true;
            }

            return child.LivesWithId.Value == parent.Id
                || !world.TryGetCharacter(child.LivesWithId.Value, out Character? keeper) || !keeper.IsAlive;
        }

        /// <summary>Hijos menores que viven en su casa (jugador.hijos_en_casa).</summary>
        public static int ChildrenAtHome(World world, Character parent) =>
            MinorChildren(world, parent).Count(c => LivesWith(world, c, parent));

        /// <summary>Hijos menores que viven con su otro papá (jugador.hijos_con_otro).</summary>
        public static int ChildrenWithOtherParent(World world, Character parent) =>
            MinorChildren(world, parent).Count(c => !LivesWith(world, c, parent));

        /// <summary>
        /// Hijos menores de papás separados que viven solo con <paramref name="parent"/> (no
        /// en cuidado compartido): por ellos puede pedir una pensión de alimentos (jugador.hijos_a_cargo).
        /// </summary>
        public static int ChildrenInCare(World world, Character parent) =>
            MinorChildren(world, parent).Count(c => ParentsApart(world, c, parent) && !c.SharedCustody && c.LivesWithId == parent.Id);

        /// <summary>Hijos menores de papás separados a los que aún no se les decide con quién viven.</summary>
        public static int ChildrenToAgree(World world, Character parent) =>
            MinorChildren(world, parent).Count(c => ParentsApart(world, c, parent) && !c.LivesWithId.HasValue && !c.SharedCustody);

        /// <summary>
        /// La otra parte del cuidado (jugador.coprogenitor): el otro papá, vivo, del hijo menor
        /// más chico de papás separados. Si no hay papás separados (volvieron, o la otra parte
        /// murió mientras había una decisión pendiente), el otro papá de su hijo menor más
        /// chico (o, si ya no tiene hijos menores, de su hijo más chico), aunque haya muerto:
        /// así una decisión que ya estaba en pantalla no se queda sin persona. Null si no
        /// tiene hijos con otra persona.
        /// </summary>
        public static Character? CoParent(World world, Character parent)
        {
            IReadOnlyList<Character> children = MinorChildren(world, parent);
            return children.Reverse().Where(c => ParentsApart(world, c, parent)).Select(c => OtherParent(world, c, parent)).FirstOrDefault()
                ?? children.Reverse().Select(c => OtherParent(world, c, parent)).FirstOrDefault(o => o != null)
                ?? world.ChildrenOf(parent.Id).Reverse().Select(c => OtherParent(world, c, parent)).FirstOrDefault(o => o != null);
        }

        /// <summary>Los hijos menores de papás separados se quedan a vivir con <paramref name="parent"/>.</summary>
        public static void KeepChildren(World world, Character parent) => Decide(world, parent, child => (parent.Id, false));

        /// <summary>Los hijos menores de papás separados se van a vivir con su otro papá.</summary>
        public static void GiveChildren(World world, Character parent) =>
            Decide(world, parent, child => (OtherParent(world, child, parent)!.Id, false));

        /// <summary>Cuidado compartido: los hijos menores de papás separados viven con los dos.</summary>
        public static void ShareCustody(World world, Character parent) => Decide(world, parent, child => (null, true));

        private static void Decide(World world, Character parent, System.Func<Character, (int? LivesWith, bool Shared)> choice)
        {
            foreach (Character child in MinorChildren(world, parent).Where(c => ParentsApart(world, c, parent)))
            {
                (int? livesWith, bool shared) = choice(child);
                child.LivesWithId = livesWith;
                child.SharedCustody = shared;
            }
        }
    }
}

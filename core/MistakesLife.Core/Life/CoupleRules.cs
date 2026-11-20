using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// La pareja por etapas (D4d1): siempre con una persona real de la vida del jugador, que
    /// avanza por lo que hacen los dos. Salir (se están conociendo), pololear (pareja formal,
    /// cada uno en su casa), vivir juntos (comparten casa, comida y gastos), unirse ante la ley
    /// (matrimonio o unión civil, con las leyes de cada país), terminar (quedan como ex; si
    /// estaban unidos, sigue el juzgado) o enviudar.
    /// </summary>
    public static class CoupleRules
    {
        /// <summary>Desde qué edad se puede salir con alguien.</summary>
        public const int MinAge = 14;

        /// <summary>Entre menores de edad, cuántos años de diferencia como mucho.</summary>
        public const int MaxMinorAgeGap = 2;

        /// <summary>La etapa con su pareja, o null si no tiene. Una pareja sin etapa guardada vive junta.</summary>
        public static CoupleStage? Stage(Character person) =>
            person.PartnerId.HasValue ? person.CoupleStage ?? CoupleStage.LivingTogether : (CoupleStage?)null;

        /// <summary>Si vive con su pareja (viva): solo entonces comparten la comida y los gastos.</summary>
        public static bool LivesWithPartner(World world, Character person) =>
            Stage(person) == CoupleStage.LivingTogether
            && world.TryGetCharacter(person.PartnerId!.Value, out Character? partner) && partner.IsAlive;

        /// <summary>Semanas que llevan en la etapa de ahora (0 si no tiene pareja o no se sabe desde cuándo).</summary>
        public static int WeeksInStage(World world, Character person) =>
            person.PartnerId.HasValue && person.CoupleSince.HasValue ? world.Date.WeeksSince(person.CoupleSince.Value) : 0;

        /// <summary>
        /// Si a <paramref name="who"/> le puede gustar alguien como <paramref name="other"/>, por
        /// su orientación y el género de los dos. Una persona asexual puede enamorarse igual: aquí
        /// cuenta como heterosexual (como al formar pareja antes de D4d1).
        /// </summary>
        public static bool AttractedTo(Character who, Character other)
        {
            Gender mine = who.GenderIdentity;
            Gender theirs = other.GenderIdentity;
            switch (who.Orientation)
            {
                case Orientation.Bisexual:
                    return true;
                case Orientation.Homosexual:
                    return mine == theirs && mine != Gender.NonBinary;
                default:
                    return mine != theirs && mine != Gender.NonBinary && theirs != Gender.NonBinary;
            }
        }

        /// <summary>
        /// Si <paramref name="person"/> podría invitar a salir a <paramref name="other"/>: los dos
        /// vivos y sin pareja, no son familia, a <paramref name="person"/> le puede gustar
        /// <paramref name="other"/>, tienen edad (desde <see cref="MinAge"/>) y un menor solo sale con
        /// otro menor de edad parecida (hasta <see cref="MaxMinorAgeGap"/> años de diferencia). Si a
        /// la otra persona le gusta, no se sabe hasta preguntar (ver <see cref="CanDate"/>).
        /// </summary>
        public static bool CouldAsk(World world, Character person, Character other) =>
            !person.PartnerId.HasValue && !other.PartnerId.HasValue && AgesFit(world, person, other)
            && !IsRelative(world, person, other) && AttractedTo(person, other);

        /// <summary>
        /// Si hay química entre dos adultos aunque alguno tenga pareja (D4d2: la tentación, los celos
        /// con razón): los dos vivos, mayores de edad, no son familia ni pareja entre ellos, y se
        /// pueden gustar los dos.
        /// </summary>
        public static bool Chemistry(World world, Character person, Character other) =>
            person.PartnerId != other.Id && person.AgeInYears(world.Date) >= 18 && other.AgeInYears(world.Date) >= 18
            && AgesFit(world, person, other) && !IsRelative(world, person, other)
            && AttractedTo(person, other) && AttractedTo(other, person);

        private static bool AgesFit(World world, Character person, Character other)
        {
            if (person.Id == other.Id || !person.IsAlive || !other.IsAlive)
            {
                return false;
            }

            int age = person.AgeInYears(world.Date);
            int otherAge = other.AgeInYears(world.Date);
            return age >= MinAge && otherAge >= MinAge && (age < 18) == (otherAge < 18)
                && !(age < 18 && Math.Abs(age - otherAge) > MaxMinorAgeGap);
        }

        /// <summary>Si pueden empezar a salir: <see cref="CouldAsk"/> y además se pueden gustar los dos.</summary>
        public static bool CanDate(World world, Character person, Character other) =>
            CouldAsk(world, person, other) && AttractedTo(other, person);

        /// <summary>Si son familia: papás, hijos, hermanos, abuelos o nietos.</summary>
        public static bool IsRelative(World world, Character a, Character b)
        {
            if (RelationshipRules.IsFamily(world, a, b))
            {
                return true;
            }

            return IsGrandparentOf(world, a, b) || IsGrandparentOf(world, b, a);
        }

        private static bool IsGrandparentOf(World world, Character grandparent, Character grandchild)
        {
            foreach (int? parentId in new[] { grandchild.MotherId, grandchild.FatherId, grandchild.SecondParentId })
            {
                if (parentId.HasValue && world.TryGetCharacter(parentId.Value, out Character? parent) && parent.IsChildOf(grandparent.Id))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Empiezan a salir. Si eran ex, dejan de serlo (lo que sienten se mantiene).</summary>
        /// <exception cref="InvalidOperationException">Si no pueden (ver <see cref="CanDate"/>).</exception>
        public static void StartDating(World world, Character person, Character other)
        {
            if (!CanDate(world, person, other))
            {
                throw new InvalidOperationException($"{person} no puede empezar a salir con {other}.");
            }

            if (person.RelationshipWith(other.Id) != null || other.RelationshipWith(person.Id) != null)
            {
                RelationshipRules.SetBond(world, person, other, RelationshipBond.None);
            }

            person.PartnerId = other.Id;
            other.PartnerId = person.Id;
            SetStage(world, person, CoupleStage.Dating);
        }

        /// <summary>Pasa a la etapa siguiente con su pareja (pololear o vivir juntos), en los dos.</summary>
        /// <exception cref="InvalidOperationException">Si no tiene pareja.</exception>
        public static void SetStage(World world, Character person, CoupleStage stage)
        {
            Character partner = person.PartnerId.HasValue && world.TryGetCharacter(person.PartnerId.Value, out Character? found) && found.IsAlive
                ? found
                : throw new InvalidOperationException($"{person} no tiene pareja.");
            foreach (Character who in new[] { person, partner })
            {
                who.CoupleStage = stage;
                who.CoupleSince = world.Date;
            }

            if (stage == CoupleStage.LivingTogether)
            {
                // Se viene a vivir a su casa.
                partner.Residence = person.Residence;
            }
        }

        /// <summary>Semanas desde que murió su pareja, o null si no ha enviudado (o tiene pareja otra vez).</summary>
        public static int? WeeksWidowed(World world, Character person)
        {
            if (person.PartnerId.HasValue || !person.LatePartnerId.HasValue
                || !world.TryGetCharacter(person.LatePartnerId.Value, out Character? late) || late.DeathDate == null)
            {
                return null;
            }

            return world.Date.WeeksSince(late.DeathDate.Value);
        }

        /// <summary>Lo que queda al terminar una pareja (o al morir uno): ya no hay etapa.</summary>
        internal static void Clear(Character person)
        {
            person.CoupleStage = null;
            person.CoupleSince = null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// El duelo (pedido de Isaias del 22-11-2026): cuando muere alguien de la vida del jugador,
    /// no solo la familia, le duele según quién era y cuánto lo quería.
    /// - Pesa más la familia cercana (mamá, papá, pareja, hijos), después hermanos y abuelos,
    ///   las amistades, y menos un ex, un colega, el jefe o un conocido. Dentro de eso, pesa
    ///   lo que el jugador sentía por esa persona.
    /// - La pena parte en el peso del duelo y baja de a poco hasta 0. Dura entre 8 semanas y
    ///   un año (con el peso más alto). Lo que hace el jugador (acompañarse, despedirse) lo acorta.
    /// - Mientras hay pena, la felicidad y la energía no pueden pasar de un techo que sube a
    ///   medida que pasa (ver <see cref="GriefSystem"/>); con menos energía rinde menos en la pega y en las notas.
    /// </summary>
    public static class GriefRules
    {
        /// <summary>Con menos peso que este, la muerte no se vive como duelo (alguien que casi no conocía).</summary>
        public const int MinimumWeight = 10;

        /// <summary>Lo que dura el duelo más liviano, en semanas.</summary>
        public const int ShortestWeeks = 8;

        /// <summary>Lo que dura el duelo más pesado (peso 100), en semanas.</summary>
        public const int LongestWeeks = GameDate.WeeksPerYear;

        /// <summary>Semanas que se recuerda un duelo después de muerta la persona (para el recuerdo del año).</summary>
        public const int RememberWeeks = 60;

        /// <summary>Cuánto baja el techo de la felicidad por cada punto de pena.</summary>
        public const double HappinessPerPain = 0.6;

        /// <summary>Cuánto baja el techo de la energía por cada punto de pena.</summary>
        public const double EnergyPerPain = 0.4;

        /// <summary>
        /// Quién era <paramref name="dead"/> para <paramref name="player"/>, o null si no era nadie de
        /// su vida: "madre", "padre", "pareja", "hijo", "hermano", "abuelo", "amistad", "rival",
        /// "ex", "jefe", "colega" o "conocido".
        /// </summary>
        public static string? RelationOf(World world, Character player, Character dead, int? partnerIdBefore)
        {
            if (dead.Id == player.MotherId || (dead.Id == player.SecondParentId && dead.Gender == Gender.Female))
            {
                return "madre";
            }

            if (dead.Id == player.FatherId || dead.Id == player.SecondParentId)
            {
                return "padre";
            }

            if (dead.Id == partnerIdBefore || dead.Id == player.PartnerId)
            {
                return "pareja";
            }

            if (dead.IsChildOf(player.Id))
            {
                return "hijo";
            }

            var parents = new[] { player.MotherId, player.FatherId }.Where(id => id.HasValue).Select(id => id!.Value).ToList();
            if (parents.Any(id => dead.IsChildOf(id)))
            {
                return "hermano";
            }

            foreach (int parentId in parents)
            {
                if (world.TryGetCharacter(parentId, out Character? parent) && (dead.Id == parent.MotherId || dead.Id == parent.FatherId))
                {
                    return "abuelo";
                }
            }

            Relationship? relationship = player.RelationshipWith(dead.Id);
            if (relationship == null)
            {
                return null;
            }

            switch (relationship.Bond)
            {
                case RelationshipBond.Friend:
                    return "amistad";
                case RelationshipBond.Rival:
                    return "rival";
                case RelationshipBond.Ex:
                    return "ex";
                case RelationshipBond.Colleague:
                    return player.Team?.BossId == dead.Id ? "jefe" : "colega";
                default:
                    return player.Team?.BossId == dead.Id ? "jefe" : "conocido";
            }
        }

        /// <summary>Cuánto le duele la muerte, de 0 a 100: la base de quién era más lo que lo quería.</summary>
        public static int Weight(World world, Character player, Character dead, string relation)
        {
            int affection = RelationshipRules.Feelings(world, player, dead).Affection;
            double weight;
            switch (relation)
            {
                case "madre":
                case "padre":
                case "pareja":
                case "hijo":
                    weight = 60 + (affection * 0.4);
                    break;
                case "hermano":
                case "abuelo":
                    weight = 30 + (affection * 0.5);
                    break;
                case "amistad":
                    weight = affection * 0.8;
                    break;
                case "ex":
                    weight = affection * 0.4;
                    break;
                default:
                    weight = affection * 0.3;
                    break;
            }

            return (int)Math.Round(Math.Max(0, Math.Min(100, weight)), MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Anota el duelo del jugador por alguien que acaba de morir, si era alguien de su vida y le
        /// duele lo suficiente. Devuelve el duelo, o null.
        /// </summary>
        public static Grief? Register(World world, Character player, Character dead, GameDate date, int? partnerIdBefore)
        {
            if (!player.IsAlive || dead.Id == player.Id || player.Griefs.Any(g => g.PersonId == dead.Id))
            {
                return null;
            }

            string? relation = RelationOf(world, player, dead, partnerIdBefore);
            if (relation == null)
            {
                return null;
            }

            int weight = Weight(world, player, dead, relation);
            if (weight < MinimumWeight)
            {
                return null;
            }

            var grief = new Grief(dead.Id, date, relation, weight);
            player.Griefs.Add(grief);
            return grief;
        }

        /// <summary>Lo que dura el duelo de este peso, en semanas, antes de lo que lo acorte.</summary>
        public static int DurationWeeks(int weight) =>
            ShortestWeeks + (int)Math.Round((LongestWeeks - ShortestWeeks) * weight / 100.0, MidpointRounding.AwayFromZero);

        /// <summary>La pena que deja este duelo hoy, de 0 a 100: parte en su peso y baja en línea recta hasta 0.</summary>
        public static int Pain(Grief grief, GameDate date)
        {
            int duration = Math.Max(1, DurationWeeks(grief.Weight) - grief.ReliefWeeks);
            int weeks = date.WeeksSince(grief.Since);
            if (weeks >= duration || weeks < 0)
            {
                return 0;
            }

            return (int)Math.Round(grief.Weight * (1 - ((double)weeks / duration)), MidpointRounding.AwayFromZero);
        }

        /// <summary>La pena más fuerte que tiene hoy (0 si no está de duelo).</summary>
        public static int CurrentPain(Character player, GameDate date) =>
            player.Griefs.Count == 0 ? 0 : player.Griefs.Max(g => Pain(g, date));

        /// <summary>El duelo por esa persona, o null.</summary>
        public static Grief? Of(Character player, int personId) => player.Griefs.FirstOrDefault(g => g.PersonId == personId);

        /// <summary>Las personas que murieron y que el jugador todavía recuerda, la más reciente primero.</summary>
        public static IReadOnlyList<Character> Departed(World world, Character player)
        {
            var people = new List<Character>();
            foreach (Grief grief in player.Griefs.OrderByDescending(g => g.Since.TotalWeeks).ThenBy(g => g.PersonId))
            {
                if (world.TryGetCharacter(grief.PersonId, out Character? dead) && !dead.IsAlive)
                {
                    people.Add(dead);
                }
            }

            return people;
        }
    }
}

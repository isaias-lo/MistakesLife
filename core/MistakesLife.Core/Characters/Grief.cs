using MistakesLife.Core.Time;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// El duelo del jugador por alguien de su gente que murió. Ver <see cref="Life.GriefRules"/>.
    /// </summary>
    public sealed class Grief
    {
        public Grief(int personId, GameDate since, string relation, int weight)
        {
            PersonId = personId;
            Since = since;
            Relation = relation;
            Weight = weight;
        }

        /// <summary>Quién murió.</summary>
        public int PersonId { get; }

        /// <summary>Cuándo murió.</summary>
        public GameDate Since { get; }

        /// <summary>Quién era para el jugador ("madre", "amistad", "jefe"...; ver <see cref="Life.GriefRules"/>).</summary>
        public string Relation { get; }

        /// <summary>Cuánto le duele, de 0 a 100: según quién era y cuánto lo quería.</summary>
        public int Weight { get; }

        /// <summary>Semanas que se acortó el duelo por lo que hizo el jugador (acompañarse, despedirse).</summary>
        public int ReliefWeeks { get; set; }
    }
}

using MistakesLife.Core.Time;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Algo que el personaje quiere en una etapa ("tener un amigo de verdad", "que mi mamá
    /// pase más tiempo conmigo"). Nace de su vida; el jugador marca con destacador los que
    /// persigue. No es una misión: puede cumplirse, perderse o pasar sin pena.
    /// </summary>
    public sealed class Wish
    {
        public Wish(string wishId, string stageKey, GameDate since, int? personId)
        {
            WishId = wishId;
            StageKey = stageKey;
            Since = since;
            PersonId = personId;
        }

        /// <summary>El deseo del paquete (carpeta deseos/), por ejemplo "deseo.amigo_de_verdad".</summary>
        public string WishId { get; }

        /// <summary>La etapa en que nació (la del horario): al cambiar, el deseo se cierra.</summary>
        public string StageKey { get; }

        public GameDate Since { get; }

        /// <summary>La persona del deseo ("con" en el paquete), si es con alguien.</summary>
        public int? PersonId { get; }

        /// <summary>Si el jugador lo marcó con destacador: lo persigue.</summary>
        public bool Highlighted { get; set; }

        public WishState State { get; internal set; }

        /// <summary>Cuándo se cumplió o se cerró; null mientras sigue abierto.</summary>
        public GameDate? Closed { get; internal set; }
    }
}

namespace MistakesLife.Core.Life
{
    /// <summary>Cómo terminó (o no) algo que el personaje quería.</summary>
    public enum WishState
    {
        /// <summary>Todavía lo quiere: la etapa sigue.</summary>
        Open = 0,

        /// <summary>Se cumplió.</summary>
        Done = 1,

        /// <summary>Lo perseguía y la etapa se acabó sin que pasara.</summary>
        Lost = 2,

        /// <summary>No lo perseguía y la etapa se acabó: se le pasó, sin pena.</summary>
        Faded = 3,
    }
}

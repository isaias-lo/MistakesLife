namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Adónde va el jugador en una semana normal, para dibujar el barrio
    /// (qué edificio se marca y el camino desde la casa). No cambia ninguna regla del juego.
    /// </summary>
    public enum DailyPlace
    {
        /// <summary>Se queda en casa (guagua, sin estudios ni trabajo, jubilado).</summary>
        Home,

        /// <summary>Jardín infantil o colegio (etapas educativas del país).</summary>
        School,

        /// <summary>Instituto o universidad (estudios de una carrera).</summary>
        Studies,

        /// <summary>Trabajo.</summary>
        Work,
    }
}

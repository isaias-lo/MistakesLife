namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Etapa de la vida según la edad, para dibujar (tamaño de la figura, cuna o cama, escritorio...).
    /// No cambia ninguna regla del juego: solo dice cómo se ve una persona.
    /// </summary>
    public enum LifeStage
    {
        /// <summary>0 y 1 año.</summary>
        Baby,

        /// <summary>2 a 11 años.</summary>
        Child,

        /// <summary>12 a 17 años.</summary>
        Teen,

        /// <summary>18 a 64 años.</summary>
        Adult,

        /// <summary>65 años o más.</summary>
        Elder,
    }
}

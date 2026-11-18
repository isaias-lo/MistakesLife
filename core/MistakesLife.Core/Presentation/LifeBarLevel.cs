namespace MistakesLife.Core.Presentation
{
    /// <summary>Cómo va una barra de la vida: se pinta verde, amarilla o roja.</summary>
    public enum LifeBarLevel
    {
        /// <summary>Baja (menos de 40): roja.</summary>
        Low,

        /// <summary>A medias (de 40 a 59): amarilla.</summary>
        Middle,

        /// <summary>Bien (60 o más): verde.</summary>
        Good,
    }
}

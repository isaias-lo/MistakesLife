namespace MistakesLife.Core.Presentation
{
    /// <summary>Una barra de la vida (Felicidad, Salud, Inteligencia...), lista para dibujar con su porcentaje.</summary>
    public sealed class LifeBarView
    {
        /// <summary>Desde este valor la barra va amarilla.</summary>
        public const int MiddleFrom = 40;

        /// <summary>Desde este valor la barra va verde.</summary>
        public const int GoodFrom = 60;

        public LifeBarView(string id, string label, int value, LifeBarLevel level)
        {
            Id = id;
            Label = label;
            Value = value;
            Level = level;
        }

        /// <summary>Qué barra es ("felicidad", "salud", "inteligencia", "apariencia", "energia"): Unity elige su ícono con esto.</summary>
        public string Id { get; }

        /// <summary>Su nombre para el jugador ("Felicidad").</summary>
        public string Label { get; }

        /// <summary>De 0 a 100: el porcentaje que se muestra.</summary>
        public int Value { get; }

        public LifeBarLevel Level { get; }

        /// <summary>El nivel de un valor de 0 a 100.</summary>
        public static LifeBarLevel LevelOf(int value) =>
            value >= GoodFrom ? LifeBarLevel.Good : value >= MiddleFrom ? LifeBarLevel.Middle : LifeBarLevel.Low;
    }
}

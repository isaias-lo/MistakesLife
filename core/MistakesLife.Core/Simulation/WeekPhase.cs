namespace MistakesLife.Core.Simulation
{
    /// <summary>
    /// Las seis fases de cada semana, en el orden en que siempre ocurren.
    /// </summary>
    public enum WeekPhase
    {
        /// <summary>Se aplica cómo repartió su tiempo el jugador y lo que eligió.</summary>
        Decisions = 0,

        /// <summary>Cada módulo actualiza lo suyo (dinero, salud, notas, relaciones).</summary>
        Systems = 1,

        /// <summary>Se simulan los demás personajes según su nivel de detalle.</summary>
        Characters = 2,

        /// <summary>Se revisan las semillas dormidas y se decide cuáles despiertan.</summary>
        Seeds = 3,

        /// <summary>Se eligen los eventos posibles; un día clave detiene el tiempo.</summary>
        Events = 4,

        /// <summary>Se aplican efectos, se plantan semillas nuevas y se escribe el diario.</summary>
        Consequences = 5,
    }
}

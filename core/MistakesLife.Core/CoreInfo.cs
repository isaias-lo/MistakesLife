namespace MistakesLife.Core
{
    /// <summary>
    /// Datos básicos del núcleo de simulación.
    /// </summary>
    public static class CoreInfo
    {
        /// <summary>Nombre del juego.</summary>
        public const string GameName = "MistakesLife";

        /// <summary>
        /// Versión del formato de guardado. Se sube cada vez que cambia la forma
        /// en que se guarda una partida, para poder migrar partidas viejas.
        /// </summary>
        public const int SaveFormatVersion = 5;
    }
}

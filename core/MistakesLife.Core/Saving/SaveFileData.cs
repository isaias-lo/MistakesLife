namespace MistakesLife.Core.Saving
{
    /// <summary>Lo que contiene un archivo de guardado: la versión del formato y el mundo.</summary>
    internal sealed class SaveFileData
    {
        /// <summary>Versión del formato (ver <see cref="CoreInfo.SaveFormatVersion"/>).</summary>
        public int Version { get; set; }

        /// <summary>Nombre del juego, para reconocer que el texto es un guardado nuestro.</summary>
        public string Game { get; set; } = string.Empty;

        /// <summary>Cuántas líneas del diario ya se le mostraron al jugador.</summary>
        public int JournalRead { get; set; }

        public WorldData World { get; set; } = new WorldData();

        /// <summary>
        /// Volver al pasado (desde la versión 5). Null dentro de una foto del mundo,
        /// que guarda solo la partida de ese momento.
        /// </summary>
        public TimeMachineData? TimeMachine { get; set; }
    }
}

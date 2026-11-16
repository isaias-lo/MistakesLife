using MistakesLife.Core.Time;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Una regla propia del jugador: "si vuelve a pasar, haz esto". Lo ya decidido no se
    /// vuelve a preguntar: cuando pasa un evento con esa regla, se elige solo y el tiempo sigue.
    /// </summary>
    public sealed class OwnRule
    {
        public OwnRule(string key, string choice)
        {
            Key = key;
            Choice = choice;
        }

        /// <summary>
        /// A qué se aplica: el grupo de eventos ("regla.prueba": cualquier prueba o tarea)
        /// o, si el evento no tiene grupo, el id del evento.
        /// </summary>
        public string Key { get; }

        /// <summary>Qué se elige: el "regla" de la opción o, si no tiene, la clave de su texto.</summary>
        public string Choice { get; }

        /// <summary>Cuántas veces se usó sola.</summary>
        public int TimesUsed { get; set; }

        /// <summary>La última vez que se usó; null si todavía no.</summary>
        public GameDate? LastUsed { get; internal set; }
    }
}

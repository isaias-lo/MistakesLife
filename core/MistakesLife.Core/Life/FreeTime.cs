using System.Collections.Generic;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Las tardes libres que el jugador ya usó este mes en acciones con el tiempo parado
    /// (<see cref="FreeTimeRules"/>), y qué hizo con ellas. Al empezar otro mes vuelve a cero.
    /// </summary>
    public sealed class FreeTime
    {
        public FreeTime(int monthKey)
        {
            MonthKey = monthKey;
        }

        /// <summary>El mes al que corresponde (<see cref="FreeTimeRules.MonthKey"/>).</summary>
        public int MonthKey { get; }

        /// <summary>Cuántas tardes libres ya gastó este mes.</summary>
        public int Used { get; set; }

        /// <summary>
        /// Lo que hizo este mes, una vez por cada vez: el id de la acción, o
        /// "id#persona" si fue con alguien (para no repetir más de lo que dice veces_al_mes).
        /// </summary>
        public List<string> Done { get; } = new List<string>();
    }
}

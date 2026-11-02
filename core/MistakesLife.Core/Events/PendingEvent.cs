using System.Collections.Generic;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Un evento que ya ocurrió y espera que el jugador elija una opción.
    /// </summary>
    public sealed class PendingEvent
    {
        public PendingEvent(string eventId, int characterId, GameDate date, int? seedId, IReadOnlyList<int> availableOptions)
        {
            EventId = eventId;
            CharacterId = characterId;
            Date = date;
            SeedId = seedId;
            AvailableOptions = availableOptions;
        }

        public string EventId { get; }

        public int CharacterId { get; }

        public GameDate Date { get; }

        /// <summary>La semilla que lo provocó, si la hay.</summary>
        public int? SeedId { get; }

        /// <summary>Índices (desde 0) de las opciones que el jugador puede elegir.</summary>
        public IReadOnlyList<int> AvailableOptions { get; }
    }
}

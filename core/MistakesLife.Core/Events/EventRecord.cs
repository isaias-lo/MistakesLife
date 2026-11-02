using MistakesLife.Core.Time;

namespace MistakesLife.Core.Events
{
    /// <summary>Un evento vivido, para el historial.</summary>
    public sealed class EventRecord
    {
        public EventRecord(GameDate date, string eventId, int characterId, int? chosenOption)
        {
            Date = date;
            EventId = eventId;
            CharacterId = characterId;
            ChosenOption = chosenOption;
        }

        public GameDate Date { get; }

        public string EventId { get; }

        public int CharacterId { get; }

        /// <summary>Índice de la opción elegida, o null si el evento no tenía opciones.</summary>
        public int? ChosenOption { get; }
    }
}

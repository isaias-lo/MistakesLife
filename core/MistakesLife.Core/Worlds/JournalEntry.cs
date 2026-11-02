using System;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Worlds
{
    /// <summary>
    /// Una línea del diario. Guarda una clave de texto (no la frase), para que
    /// el diario se pueda mostrar en cualquier idioma.
    /// </summary>
    public sealed class JournalEntry
    {
        public JournalEntry(GameDate date, int characterId, string textKey)
        {
            Date = date;
            CharacterId = characterId;
            TextKey = textKey ?? throw new ArgumentNullException(nameof(textKey));
        }

        public GameDate Date { get; }

        public int CharacterId { get; }

        public string TextKey { get; }
    }
}

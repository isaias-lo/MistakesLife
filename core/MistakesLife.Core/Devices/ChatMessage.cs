using System;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// Un mensaje de una conversación de Mensajito: cuándo se mandó, su clave de texto
    /// (el id de packs/*/chats/ o de una de sus respuestas) y si lo mandó el jugador.
    /// </summary>
    public sealed class ChatMessage
    {
        public ChatMessage(GameDate date, string textKey, bool fromPlayer)
        {
            Date = date;
            TextKey = textKey ?? throw new ArgumentNullException(nameof(textKey));
            FromPlayer = fromPlayer;
        }

        public GameDate Date { get; }

        public string TextKey { get; }

        public bool FromPlayer { get; }
    }
}

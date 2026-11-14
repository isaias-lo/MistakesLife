using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Una conversación de Mensajito guardada (desde la versión 17).</summary>
    internal sealed class ChatData
    {
        public int OtherId { get; set; }

        public int Unread { get; set; }

        public string? AwaitingReply { get; set; }

        public int AwaitingSinceWeek { get; set; }

        /// <summary>Lo que el jugador escribió primero y espera respuesta, o null (desde la versión 18).</summary>
        public string? PlayerWrote { get; set; }

        public int PlayerWroteWeek { get; set; }

        public List<ChatMessageData> Messages { get; set; } = new List<ChatMessageData>();
    }
}

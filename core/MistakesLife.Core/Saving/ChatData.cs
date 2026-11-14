using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Una conversación de Mensajito guardada (desde la versión 15).</summary>
    internal sealed class ChatData
    {
        public int OtherId { get; set; }

        public int Unread { get; set; }

        public string? AwaitingReply { get; set; }

        public int AwaitingSinceWeek { get; set; }

        public List<ChatMessageData> Messages { get; set; } = new List<ChatMessageData>();
    }
}

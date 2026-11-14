using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Las conversaciones de Mensajito guardadas (desde la versión 15).</summary>
    internal sealed class ChatInboxData
    {
        public int JournalCursor { get; set; }

        public List<ChatData> Chats { get; set; } = new List<ChatData>();
    }
}

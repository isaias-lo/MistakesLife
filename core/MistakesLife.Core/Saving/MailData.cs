using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Una cuenta de correo guardada (desde la versión 10).</summary>
    internal sealed class MailData
    {
        public int SinceWeek { get; set; }

        public int JournalCursor { get; set; }

        public List<MailMessageData> Messages { get; set; } = new List<MailMessageData>();
    }
}

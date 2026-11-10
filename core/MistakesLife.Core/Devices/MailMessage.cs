using System;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// Un correo que llegó a la bandeja: cuándo llegó, cuál es (id de
    /// packs/*/mail/, cuyas claves de texto son el id, id + ".asunto" y su
    /// remitente) y si ya se leyó.
    /// </summary>
    public sealed class MailMessage
    {
        public MailMessage(GameDate date, string mailId, bool read)
        {
            Date = date;
            MailId = mailId ?? throw new ArgumentNullException(nameof(mailId));
            Read = read;
        }

        public GameDate Date { get; }

        public string MailId { get; }

        public bool Read { get; internal set; }
    }
}

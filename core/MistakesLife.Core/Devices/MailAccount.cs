using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// La cuenta de correo de un personaje: su bandeja de entrada. Ahí llegan los
    /// avisos de la vida (llamados a entrevista, cobranzas, impuestos, la
    /// universidad, el juzgado) según packs/*/mail/. Ver <see cref="MailSystem"/>.
    /// </summary>
    public sealed class MailAccount
    {
        /// <summary>Cuántos correos guarda la bandeja; al pasarse, se borra el más antiguo ya leído (o el más antiguo).</summary>
        public const int MaxMessages = 300;

        private readonly List<MailMessage> _messages = new List<MailMessage>();

        /// <param name="since">Cuándo creó la cuenta.</param>
        /// <param name="journalCursor">
        /// Hasta qué línea del diario del mundo ya se revisó (al crearla, el largo
        /// del diario: los correos nunca llegan por cosas de antes de tener cuenta).
        /// </param>
        public MailAccount(GameDate since, int journalCursor)
        {
            if (journalCursor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(journalCursor));
            }

            Since = since;
            JournalCursor = journalCursor;
        }

        public GameDate Since { get; }

        /// <summary>Hasta qué línea del diario del mundo ya llegaron los correos.</summary>
        public int JournalCursor { get; internal set; }

        /// <summary>Los correos, del más antiguo al más nuevo.</summary>
        public IReadOnlyList<MailMessage> Messages => _messages;

        public int UnreadCount => _messages.Count(m => !m.Read);

        /// <summary>Llega un correo nuevo, sin leer.</summary>
        public void Receive(GameDate date, string mailId) => Add(new MailMessage(date, mailId, false));

        /// <summary>Marca como leído el correo en esa posición de <see cref="Messages"/>.</summary>
        public void MarkRead(int index)
        {
            if (index < 0 || index >= _messages.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"No hay correo número {index}.");
            }

            _messages[index].Read = true;
        }

        public void MarkAllRead()
        {
            foreach (MailMessage message in _messages)
            {
                message.Read = true;
            }
        }

        /// <summary>Agrega un correo tal como estaba (al cargar una partida).</summary>
        internal void Add(MailMessage message)
        {
            _messages.Add(message);
            if (_messages.Count > MaxMessages)
            {
                int oldestRead = _messages.FindIndex(m => m.Read);
                _messages.RemoveAt(oldestRead >= 0 ? oldestRead : 0);
            }
        }
    }
}

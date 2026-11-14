using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// Las conversaciones de Mensajito de un personaje, una por persona. Como el correo,
    /// es una cuenta: si pierde el celular, al tener uno nuevo con la app recupera sus
    /// chats. Ver <see cref="ChatSystem"/>.
    /// </summary>
    public sealed class ChatInbox
    {
        /// <summary>Cuántas conversaciones guarda; al pasarse, se borra la que lleva más tiempo sin mensajes.</summary>
        public const int MaxChats = 12;

        private readonly List<Chat> _chats = new List<Chat>();

        /// <param name="journalCursor">
        /// Hasta qué línea del diario del mundo ya se revisó (al crearla, el largo del
        /// diario: nadie escribe por cosas de antes de tener la app).
        /// </param>
        public ChatInbox(int journalCursor)
        {
            if (journalCursor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(journalCursor));
            }

            JournalCursor = journalCursor;
        }

        /// <summary>Hasta qué línea del diario del mundo ya llegaron los mensajes.</summary>
        public int JournalCursor { get; internal set; }

        /// <summary>Las conversaciones, en el orden en que empezaron.</summary>
        public IReadOnlyList<Chat> Chats => _chats;

        /// <summary>Cuántos mensajes sin ver hay entre todas las conversaciones.</summary>
        public int UnreadCount => _chats.Sum(c => c.Unread);

        /// <summary>La conversación con esa persona, o null si nunca se han escrito.</summary>
        public Chat? With(int otherId) => _chats.Find(c => c.OtherId == otherId);

        /// <summary>La conversación con esa persona; si no existe, la empieza.</summary>
        public Chat Open(int otherId)
        {
            Chat? chat = With(otherId);
            if (chat == null)
            {
                chat = new Chat(otherId);
                _chats.Add(chat);
                if (_chats.Count > MaxChats)
                {
                    Chat oldest = _chats.Where(c => c != chat && c.AwaitingReply == null).OrderBy(c => c.LastDate).FirstOrDefault()
                        ?? _chats.Where(c => c != chat).OrderBy(c => c.LastDate).First();
                    _chats.Remove(oldest);
                }
            }

            return chat;
        }

        /// <summary>Llega un mensaje de la otra persona. Si espera respuesta, queda esperando desde hoy.</summary>
        public void Receive(int fromId, GameDate date, string chatId, bool expectsReply)
        {
            Chat chat = Open(fromId);
            chat.Add(new ChatMessage(date, chatId, fromPlayer: false));
            chat.Unread++;
            if (expectsReply)
            {
                chat.AwaitingReply = chatId;
                chat.AwaitingSince = date;
            }
        }

        /// <summary>El jugador abre la conversación: ya vio todo.</summary>
        public void MarkRead(int otherId)
        {
            Chat? chat = With(otherId);
            if (chat != null)
            {
                chat.Unread = 0;
            }
        }

        /// <summary>Agrega una conversación tal como estaba (al cargar una partida).</summary>
        internal void Add(Chat chat) => _chats.Add(chat);
    }
}

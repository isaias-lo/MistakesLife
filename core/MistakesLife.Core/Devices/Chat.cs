using System;
using System.Collections.Generic;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// La conversación de Mensajito con una persona real (<see cref="OtherId"/>): sus
    /// mensajes, cuántos no ha leído el jugador y, si la persona le preguntó algo, qué
    /// mensaje espera respuesta y desde cuándo.
    /// </summary>
    public sealed class Chat
    {
        /// <summary>
        /// Cuántos mensajes guarda una conversación; al pasarse, se borra el más antiguo (así el
        /// guardado, con sus fotos para volver al pasado, sigue liviano).
        /// </summary>
        public const int MaxMessages = 15;

        private readonly List<ChatMessage> _messages = new List<ChatMessage>();

        public Chat(int otherId)
        {
            OtherId = otherId;
        }

        /// <summary>Con quién es la conversación.</summary>
        public int OtherId { get; }

        /// <summary>Los mensajes, del más antiguo al más nuevo.</summary>
        public IReadOnlyList<ChatMessage> Messages => _messages;

        /// <summary>Cuántos mensajes de la otra persona no ha visto el jugador.</summary>
        public int Unread { get; internal set; }

        /// <summary>El mensaje (id de packs/*/chats/) que espera respuesta, o null.</summary>
        public string? AwaitingReply { get; internal set; }

        /// <summary>Desde cuándo espera respuesta (si <see cref="AwaitingReply"/> no es null).</summary>
        public GameDate AwaitingSince { get; internal set; }

        /// <summary>La fecha del último mensaje (para ordenar la lista de chats).</summary>
        public GameDate LastDate => _messages.Count == 0 ? default : _messages[_messages.Count - 1].Date;

        /// <summary>Agrega un mensaje al final.</summary>
        internal void Add(ChatMessage message)
        {
            _messages.Add(message ?? throw new ArgumentNullException(nameof(message)));
            if (_messages.Count > MaxMessages)
            {
                _messages.RemoveAt(0);
            }
        }
    }
}

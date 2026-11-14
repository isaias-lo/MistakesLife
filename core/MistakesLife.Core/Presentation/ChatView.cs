using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Una conversación de Mensajito abierta: con quién, los mensajes (del más antiguo al
    /// más nuevo) y, si la persona espera respuesta, lo que el jugador puede contestar
    /// (con <see cref="LifePresenter.ReplyChat"/>, por su posición en <see cref="Replies"/>).
    /// </summary>
    public sealed class ChatView
    {
        public ChatView(int otherId, string name, string relation, bool isFemale, IReadOnlyList<ChatBubbleView> messages, IReadOnlyList<string> replies, bool replyAfterEvent = false)
        {
            OtherId = otherId;
            Name = name;
            Relation = relation;
            IsFemale = isFemale;
            Messages = messages;
            Replies = replies;
            ReplyAfterEvent = replyAfterEvent;
        }

        public int OtherId { get; }

        public string Name { get; }

        public string Relation { get; }

        public bool IsFemale { get; }

        public IReadOnlyList<ChatBubbleView> Messages { get; }

        /// <summary>Las respuestas posibles; vacía si no hay nada que contestar.</summary>
        public IReadOnlyList<string> Replies { get; }

        /// <summary>Si hay algo que contestar, pero primero hay que decidir el evento que está pasando.</summary>
        public bool ReplyAfterEvent { get; }
    }
}

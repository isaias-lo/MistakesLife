namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Una fila de la lista de chats de Mensajito: con quién (como lo tendría guardado
    /// el jugador: "Mamá", o su nombre), qué es para el jugador, el último mensaje,
    /// cuándo fue, cuántos no ha visto y si esa persona espera una respuesta.
    /// </summary>
    public sealed class ChatSummaryView
    {
        public ChatSummaryView(int otherId, string name, string relation, string lastMessage, bool lastFromPlayer, string when, int unread, bool awaitingReply, bool isFemale)
        {
            OtherId = otherId;
            Name = name;
            Relation = relation;
            LastMessage = lastMessage;
            LastFromPlayer = lastFromPlayer;
            When = when;
            Unread = unread;
            AwaitingReply = awaitingReply;
            IsFemale = isFemale;
        }

        /// <summary>Con quién es (para abrir la conversación con <see cref="LifePresenter.Chat"/>).</summary>
        public int OtherId { get; }

        public string Name { get; }

        /// <summary>"Mamá", "Amistad", "Ex"... o vacío.</summary>
        public string Relation { get; }

        /// <summary>El último mensaje, corto (ver <see cref="MailView.Preview"/>).</summary>
        public string LastMessage { get; }

        public bool LastFromPlayer { get; }

        /// <summary>"Esta semana", "Hace 3 semanas"...</summary>
        public string When { get; }

        public int Unread { get; }

        public bool AwaitingReply { get; }

        /// <summary>Para el color de su foto de perfil.</summary>
        public bool IsFemale { get; }
    }
}

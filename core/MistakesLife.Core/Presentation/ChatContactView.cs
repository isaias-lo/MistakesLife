namespace MistakesLife.Core.Presentation
{
    /// <summary>Una persona a la que el jugador le puede escribir (la lista de "Nuevo chat").</summary>
    public sealed class ChatContactView
    {
        public ChatContactView(int otherId, string name, string relation, bool isFemale)
        {
            OtherId = otherId;
            Name = name;
            Relation = relation;
            IsFemale = isFemale;
        }

        public int OtherId { get; }

        public string Name { get; }

        public string Relation { get; }

        public bool IsFemale { get; }
    }
}

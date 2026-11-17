using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;

namespace MistakesLife.Core.Life
{
    /// <summary>Una acción que el jugador puede hacer ahora, con la persona con quien la haría (si lleva "con").</summary>
    public sealed class ActionOption
    {
        public ActionOption(ActionDefinition definition, Character? person)
        {
            Definition = definition;
            Person = person;
        }

        public ActionDefinition Definition { get; }

        public Character? Person { get; }
    }
}

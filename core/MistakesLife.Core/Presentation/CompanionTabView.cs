using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Una pestaña al elegir con quién (D5c2, boceto BElegir): "Amigos 12", con su gente en filas grandes, primero los más cercanos.</summary>
    public sealed class CompanionTabView
    {
        public CompanionTabView(string key, string label, IReadOnlyList<CompanionView> people)
        {
            Key = key;
            Label = label;
            People = people;
        }

        /// <summary>pareja, familia, amigos, curso, pega o conocidos.</summary>
        public string Key { get; }

        /// <summary>"Amigos".</summary>
        public string Label { get; }

        public IReadOnlyList<CompanionView> People { get; }
    }
}

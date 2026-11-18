using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Tu gente (D4a): los cercanos arriba (familia, pareja, amistades, rivales y ex) y los
    /// conocidos abajo, en una sección que parte cerrada (tu curso, tus colegas, tu profe, la
    /// gente que conoces de algún momento).
    /// </summary>
    public sealed class PeopleView
    {
        public PeopleView(IReadOnlyList<PersonRowView> close, IReadOnlyList<PersonRowView> acquaintances, string acquaintancesLabel, string acquaintancesNote)
        {
            Close = close;
            Acquaintances = acquaintances;
            AcquaintancesLabel = acquaintancesLabel;
            AcquaintancesNote = acquaintancesNote;
        }

        /// <summary>Los cercanos, primero los vivos y por lo unidos que son; los que murieron, al final.</summary>
        public IReadOnlyList<PersonRowView> Close { get; }

        /// <summary>Los conocidos, por lo unidos que son.</summary>
        public IReadOnlyList<PersonRowView> Acquaintances { get; }

        /// <summary>"Conocidos · 31".</summary>
        public string AcquaintancesLabel { get; }

        /// <summary>De dónde son: "tu curso, tus colegas y tu profe".</summary>
        public string AcquaintancesNote { get; }
    }
}

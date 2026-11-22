using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Con quién hacer algo en un lugar del barrio (D5c2): nunca una lista larga con letra chica.
    /// Arriba, <see cref="LifePresenter.MaxSuggested"/> sugeridos grandes con su motivo; abajo,
    /// "Elegir a otra persona", con pestañas por grupo (y el buscador en la pantalla).
    /// </summary>
    public sealed class CompanionsView
    {
        public CompanionsView(string actionId, string title, IReadOnlyList<CompanionView> suggested, IReadOnlyList<CompanionTabView> tabs, int total, string chooseOther, string? alone)
        {
            ActionId = actionId;
            Title = title;
            Suggested = suggested;
            Tabs = tabs;
            Total = total;
            ChooseOther = chooseOther;
            Alone = alone;
        }

        public string ActionId { get; }

        /// <summary>"Ver una película".</summary>
        public string Title { get; }

        /// <summary>Los sugeridos, con su motivo.</summary>
        public IReadOnlyList<CompanionView> Suggested { get; }

        /// <summary>Las pestañas con gente (las vacías no salen).</summary>
        public IReadOnlyList<CompanionTabView> Tabs { get; }

        /// <summary>Con cuántas personas se puede hacer.</summary>
        public int Total { get; }

        /// <summary>"Elegir a otra persona · 54 en tu gente".</summary>
        public string ChooseOther { get; }

        /// <summary>"Ir sola" si también se puede sin nadie; null si no.</summary>
        public string? Alone { get; }
    }
}

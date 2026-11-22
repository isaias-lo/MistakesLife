using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Algo que puedes hacer en un lugar del barrio (D5c2, boceto BLugar, "1 · Qué haces"): lo que
    /// cuesta (tardes, energía, plata con los precios de tu país) y lo bueno que trae. Con quién se
    /// elige después con <see cref="LifePresenter.Companions"/>, y se hace con <see cref="LifePresenter.DoAction"/>.
    /// </summary>
    public sealed class PlaceActivityView
    {
        public PlaceActivityView(string id, string title, string why, IReadOnlyList<string> costs, IReadOnlyList<string> gives, bool canAfford, string? blocked, bool canGoAlone, bool withSomeone, int companions)
        {
            Id = id;
            Title = title;
            Why = why;
            Costs = costs;
            Gives = gives;
            CanAfford = canAfford;
            Blocked = blocked;
            CanGoAlone = canGoAlone;
            WithSomeone = withSomeone;
            Companions = companions;
        }

        /// <summary>El id de la acción.</summary>
        public string Id { get; }

        /// <summary>"Ver una película".</summary>
        public string Title { get; }

        /// <summary>"Entrada para ti; cada uno paga la suya."</summary>
        public string Why { get; }

        /// <summary>Lo que cuesta, en etiquetas: "1 tarde", "Energía −1", "$6.000" o "Lo paga tu casa".</summary>
        public IReadOnlyList<string> Costs { get; }

        /// <summary>Lo bueno que trae: "+ relación", "Felicidad +4".</summary>
        public IReadOnlyList<string> Gives { get; }

        /// <summary>Si te alcanzan las tardes y la plata.</summary>
        public bool CanAfford { get; }

        /// <summary>Por qué no se puede ahora ("No te quedan tardes este mes"); null si se puede.</summary>
        public string? Blocked { get; }

        /// <summary>Si se puede ir sin nadie.</summary>
        public bool CanGoAlone { get; }

        /// <summary>Si se hace con alguien (entonces se elige con quién).</summary>
        public bool WithSomeone { get; }

        /// <summary>Con cuántas personas se puede hacer ahora.</summary>
        public int Companions { get; }
    }
}

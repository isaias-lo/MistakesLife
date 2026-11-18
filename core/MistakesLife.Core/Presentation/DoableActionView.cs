using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Algo que puedes hacer ahora con el tiempo parado, en un grupo (D3): su nombre, por qué
    /// tiene sentido ahora, lo que te da y lo que te cuesta. Se hace con
    /// <see cref="LifePresenter.DoAction"/> pasando <see cref="Id"/> y <see cref="PersonId"/>.
    /// </summary>
    public sealed class DoableActionView
    {
        public DoableActionView(string id, int? personId, string title, string why, IReadOnlyList<ChangeView> gives, string cost, bool canAfford)
        {
            Id = id;
            PersonId = personId;
            Title = title;
            Why = why;
            Gives = gives;
            Cost = cost;
            CanAfford = canAfford;
        }

        public string Id { get; }

        /// <summary>Con quién (si la acción es con alguien).</summary>
        public int? PersonId { get; }

        /// <summary>"Juntarte con la Javiera".</summary>
        public string Title { get; }

        /// <summary>"Hace dos semanas que no se ven".</summary>
        public string Why { get; }

        /// <summary>Lo que cambia, en verde lo bueno y en rojo lo que cuesta ("Inteligencia +2", "Energía −3").</summary>
        public IReadOnlyList<ChangeView> Gives { get; }

        /// <summary>"1 tarde".</summary>
        public string Cost { get; }

        /// <summary>Si le quedan tardes libres para hacerla este mes.</summary>
        public bool CanAfford { get; }
    }
}

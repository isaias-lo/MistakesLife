using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// El mapa chico de tu barrio (D5c2, boceto BMapa): lo que se abre al tocar Tu gente. Sus
    /// lugares (los de base y los de tu país), tus tardes libres y la gente más cercana para
    /// hablarle al tiro. Se arma con <see cref="LifePresenter.Neighborhood"/>.
    /// </summary>
    public sealed class NeighborhoodView
    {
        public NeighborhoodView(string title, string hint, string peopleHint, FreeTimeView? freeTime, IReadOnlyList<MapPlaceView> places, IReadOnlyList<CompanionView> closest)
        {
            Title = title;
            Hint = hint;
            PeopleHint = peopleHint;
            FreeTime = freeTime;
            Places = places;
            Closest = closest;
        }

        /// <summary>"Tu gente · Puente Alto".</summary>
        public string Title { get; }

        /// <summary>"Toca un lugar para ir".</summary>
        public string Hint { get; }

        /// <summary>La línea de abajo, junto a tu gente más cercana.</summary>
        public string PeopleHint { get; }

        public FreeTimeView? FreeTime { get; }

        /// <summary>Los lugares que hay ahora, en el orden de los paquetes (primero los de base).</summary>
        public IReadOnlyList<MapPlaceView> Places { get; }

        /// <summary>Las <see cref="LifePresenter.MaxClosest"/> personas más cercanas, para tocarlas y abrir su ficha.</summary>
        public IReadOnlyList<CompanionView> Closest { get; }

        /// <summary>Cambia cuando cambian los lugares (para no volver a armar el mapa 3D si no cambió).</summary>
        public string Signature => string.Join("|", Places.Count, string.Join(",", System.Linq.Enumerable.Select(Places, p => p.Id)));
    }
}

using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// La ficha de una persona (D4a, boceto D4Ficha): arriba su cara, nombre, qué es tuyo, edad y
    /// comuna con la barra de la relación; después su vida, lo que han vivido juntos y lo que
    /// pueden hacer (lo que gasta tardes libres y lo que se pide como momento).
    /// </summary>
    public sealed class PersonCardView
    {
        public PersonCardView(PersonRowView person, string subtitle, string life, IReadOnlyList<LivedView> lived, IReadOnlyList<DoableActionView> actions, IReadOnlyList<ActionView> requests, IReadOnlyList<string> offers, bool offersHidden)
        {
            Offers = offers;
            OffersHidden = offersHidden;
            Person = person;
            Subtitle = subtitle;
            Life = life;
            Lived = lived;
            Actions = actions;
            Requests = requests;
        }

        public PersonRowView Person { get; }

        /// <summary>"Tu mamá · 41 años · Puente Alto".</summary>
        public string Subtitle { get; }

        /// <summary>Su vida, en una o dos frases; vacío si el juego no sabe nada más de esa persona.</summary>
        public string Life { get; }

        /// <summary>Lo que han vivido juntos, lo más nuevo primero (hasta <see cref="LifePresenter.MaxLived"/>).</summary>
        public IReadOnlyList<LivedView> Lived { get; }

        /// <summary>Lo que pueden hacer juntos con el tiempo parado (gasta tardes libres, D3).</summary>
        public IReadOnlyList<DoableActionView> Actions { get; }

        /// <summary>
        /// Lo que te puede aportar (D4b2), en frases: lo que de verdad puede hacer por ti según su
        /// vida (un consejo de lo tuyo, recomendarte para un ascenso, prestarte plata). Vacío si
        /// nada en especial o si todavía no lo sabes (<see cref="OffersHidden"/>).
        /// </summary>
        public IReadOnlyList<string> Offers { get; }

        /// <summary>Si lo que te puede aportar todavía está oculto: es gente interesante con quien no has conversado.</summary>
        public bool OffersHidden { get; }

        /// <summary>Lo que le puedes proponer (recomendarla en tu pega, vivir juntos...): se decide en la carta del momento.</summary>
        public IReadOnlyList<ActionView> Requests { get; }
    }
}

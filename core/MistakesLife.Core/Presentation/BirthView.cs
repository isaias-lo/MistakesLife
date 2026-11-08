using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Cómo nace una vida, para mostrarla antes de empezar a jugarla:
    /// el jugador elige entre algunos nacimientos posibles (como en la vida real, no elige
    /// a su familia, pero sí cuál de estas historias quiere vivir).
    /// </summary>
    public sealed class BirthView
    {
        public BirthView(string name, bool isFemale, string sexLabel, string place, string homeLabel, IReadOnlyList<PersonView> parents)
        {
            Name = name;
            IsFemale = isFemale;
            SexLabel = sexLabel;
            Place = place;
            HomeLabel = homeLabel;
            Parents = parents;
        }

        /// <summary>Nombre y apellido de quien nace.</summary>
        public string Name { get; }

        public bool IsFemale { get; }

        /// <summary>"Niña" o "Niño", en el idioma del jugador.</summary>
        public string SexLabel { get; }

        /// <summary>Comuna, ciudad y país donde nace.</summary>
        public string Place { get; }

        /// <summary>Cómo es el hogar en una frase ("Hogar de clase media").</summary>
        public string HomeLabel { get; }

        /// <summary>La mamá y el papá (si los hay), con su edad.</summary>
        public IReadOnlyList<PersonView> Parents { get; }
    }
}

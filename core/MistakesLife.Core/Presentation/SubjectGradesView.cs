using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Un ramo en tu libreta: sus notas y su promedio, ya escritos como en el país.</summary>
    public sealed class SubjectGradesView
    {
        public SubjectGradesView(string name, IReadOnlyList<string> marks, string average, bool failing)
        {
            Name = name;
            Marks = marks;
            Average = average;
            Failing = failing;
        }

        /// <summary>El ramo ("Matemática").</summary>
        public string Name { get; }

        /// <summary>Las notas en el orden en que llegaron ("5,8", "6,2"...).</summary>
        public IReadOnlyList<string> Marks { get; }

        /// <summary>El promedio del ramo ("6,0").</summary>
        public string Average { get; }

        /// <summary>Si el promedio va bajo la nota para aprobar (se escribe en rojo).</summary>
        public bool Failing { get; }
    }
}

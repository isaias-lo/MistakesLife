using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Tu curso del colegio, listo para mostrar ("8º básico B", tu profe jefe y tus compañeros).</summary>
    public sealed class SchoolClassView
    {
        public SchoolClassView(string name, int students, string? teacher, IReadOnlyList<ClassmateView> classmates)
        {
            Name = name;
            Students = students;
            Teacher = teacher;
            Classmates = classmates;
        }

        /// <summary>El curso, como se dice en el país ("8º básico B").</summary>
        public string Name { get; }

        /// <summary>Cuántos alumnos tiene, contándote.</summary>
        public int Students { get; }

        /// <summary>La profe o el profe jefe ("la profe Carolina Soto"); null si no hay.</summary>
        public string? Teacher { get; }

        /// <summary>Los compañeros con nombre, del que más quieres al que menos.</summary>
        public IReadOnlyList<ClassmateView> Classmates { get; }
    }
}

using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Tu curso del colegio, listo para mostrar ("8º básico B", tu profe jefe y tus compañeros).</summary>
    public sealed class SchoolClassView
    {
        public SchoolClassView(string name, int students, string? teacher, IReadOnlyList<ClassmateView> classmates, bool section = false, IReadOnlyList<ClassmateView>? left = null)
        {
            Left = left ?? new List<ClassmateView>();
            Section = section;
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

        /// <summary>Si es la sección de la universidad, el CFT o el instituto (y no un curso del colegio).</summary>
        public bool Section { get; }

        /// <summary>Los compañeros con nombre, del que más quieres al que menos.</summary>
        public IReadOnlyList<ClassmateView> Classmates { get; }

        /// <summary>Quienes dejaron la sección al cerrar el último semestre (se muestran en gris, con "Se fue").</summary>
        public IReadOnlyList<ClassmateView> Left { get; }
    }
}

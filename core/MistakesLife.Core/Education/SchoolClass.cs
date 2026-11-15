using System.Collections.Generic;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// El curso del jugador en el colegio ("8º básico B"): sus compañeros con nombre,
    /// su profe jefe y en qué año va. Se arma al entrar a una etapa con curso (básica,
    /// media) y avanza cada año, el día que empiezan las clases. Lo lleva <see cref="SchoolSystem"/>.
    /// </summary>
    public sealed class SchoolClass
    {
        public SchoolClass(string stageId, int level, string letter, int schoolYear, int students)
        {
            StageId = stageId;
            Level = level;
            Letter = letter;
            SchoolYear = schoolYear;
            Students = students;
        }

        /// <summary>La etapa del curso ("etapa.chile.basica").</summary>
        public string StageId { get; }

        /// <summary>En qué año de la etapa va (1 = primero).</summary>
        public int Level { get; internal set; }

        /// <summary>La letra del curso ("B").</summary>
        public string Letter { get; }

        /// <summary>El año escolar en que se armó o avanzó por última vez (para saber cuándo pasa de curso).</summary>
        public int SchoolYear { get; internal set; }

        /// <summary>Cuántos alumnos tiene el curso, contándote.</summary>
        public int Students { get; }

        /// <summary>La profe o el profe jefe (id de personaje), si hay.</summary>
        public int? TeacherId { get; internal set; }

        /// <summary>Los compañeros con nombre, por id, en el orden en que llegaron.</summary>
        public List<int> ClassmateIds { get; } = new List<int>();

        /// <summary>Quienes dejaron la sección al cerrar el último semestre (para verlos en gris, con "Se fue").</summary>
        public List<int> LeftIds { get; } = new List<int>();
    }
}

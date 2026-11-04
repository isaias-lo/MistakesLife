using System;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Lo que un personaje está estudiando ahora: una carrera en un nivel
    /// (por ejemplo, contador auditor en un instituto profesional).
    /// </summary>
    public sealed class CareerStudies
    {
        /// <summary>Semanas de un semestre académico.</summary>
        public const int WeeksPerSemester = 26;

        public CareerStudies(string careerId, string levelId, string institution, int requiredSemesters)
        {
            CareerId = careerId ?? throw new ArgumentNullException(nameof(careerId));
            LevelId = levelId ?? throw new ArgumentNullException(nameof(levelId));
            Institution = institution ?? throw new ArgumentNullException(nameof(institution));
            RequiredSemesters = requiredSemesters;
        }

        /// <summary>La carrera, por ejemplo "carrera.contabilidad".</summary>
        public string CareerId { get; }

        /// <summary>El nivel, por ejemplo "carrera.contabilidad.nivel.contador_auditor".</summary>
        public string LevelId { get; }

        /// <summary>Dónde estudia: "cft", "instituto" o "universidad".</summary>
        public string Institution { get; }

        /// <summary>Semestres que dura el plan de estudios (la duración formal).</summary>
        public int RequiredSemesters { get; }

        /// <summary>
        /// Semestres aprobados. El contenido puede restar uno si se reprueba,
        /// así la carrera dura más que lo formal, como en la vida real.
        /// </summary>
        public int CompletedSemesters
        {
            get => _completedSemesters;
            set => _completedSemesters = Math.Max(0, value);
        }

        /// <summary>Semanas cursadas del semestre actual.</summary>
        public int WeeksIntoSemester { get; internal set; }

        private int _completedSemesters;

        /// <summary>Avanza una semana; al completar un semestre lo suma a los aprobados.</summary>
        public void AdvanceWeek()
        {
            WeeksIntoSemester++;
            if (WeeksIntoSemester >= WeeksPerSemester)
            {
                WeeksIntoSemester = 0;
                CompletedSemesters++;
            }
        }
    }
}

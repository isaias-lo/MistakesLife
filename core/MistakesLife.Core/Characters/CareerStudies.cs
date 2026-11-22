using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Semestres con notas ya puestas. Cuando se aprueba uno nuevo (más aprobados que
        /// con notas), se le ponen las notas de sus ramos.
        /// </summary>
        public int GradedSemesters { get; internal set; }

        /// <summary>Cuánto se preparó este semestre para los certámenes (de -3 a 3), por lo que decidió en sus momentos (<c>jugador.estudiar_semestre(n)</c>).</summary>
        public int Preparation { get; internal set; }

        /// <summary>Las notas finales del último semestre con notas: ramo → nota (en la escala del país).</summary>
        public SortedDictionary<string, double> LastMarks { get; } = new SortedDictionary<string, double>(StringComparer.Ordinal);

        /// <summary>
        /// Semestres cursados (aprobados o no): la gratuidad y los créditos miran cuántos
        /// semestres lleva estudiando, no cuántos aprobó.
        /// </summary>
        public int SemestersTaken { get; internal set; }

        /// <summary>El semestre del plan al que son las <see cref="LastMarks"/> (0 si todavía no hay notas).</summary>
        public int LastMarksSemester { get; internal set; }

        /// <summary>
        /// Las entregas de los proyectos de este semestre (D5a4): ramo → calidad de cada entrega,
        /// de 0 a 100, en orden. Se vacía al poner las notas del semestre.
        /// </summary>
        public SortedDictionary<string, List<int>> ProjectDeliveries { get; } = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);

        /// <summary>Promedios de cada semestre con notas, en orden.</summary>
        public List<double> SemesterAverages { get; } = new List<double>();

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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un ramo de una carrera ("ramo.software.programacion"), en los semestres en que se
    /// tiene (de "desde" a "hasta", contando desde 1). Si se tiene en varios semestres, cada
    /// uno lleva su número: Programación I, II, III. Su nombre va en el idioma del país.
    /// </summary>
    public sealed class CourseDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Primer semestre en que se tiene (1 si falta).</summary>
        [JsonProperty("desde")]
        public int FromSemester { get; set; } = 1;

        /// <summary>Último semestre en que se tiene (el mismo de "desde" si falta).</summary>
        [JsonProperty("hasta")]
        public int? ToSemester { get; set; }

        /// <summary>
        /// Solo en estas instituciones ("cft", "instituto", "universidad"); en todas si falta.
        /// La misma carrera tiene más matemáticas en la universidad que en un instituto.
        /// </summary>
        [JsonProperty("instituciones")]
        public List<string> Institutions { get; set; } = new List<string>();

        /// <summary>
        /// Si es el ramo del último semestre de cada plan (el proyecto o el seminario de
        /// título), sea cual sea el largo de la carrera. Entonces "desde" y "hasta" no cuentan.
        /// </summary>
        [JsonProperty("ultimo")]
        public bool Last { get; set; }

        /// <summary>
        /// Si tiene pruebas escritas que se estudian con preguntas ("prueba_escrita", true si falta).
        /// Los talleres, los electivos y los seminarios se evalúan con proyectos: no llevan preguntas.
        /// </summary>
        [JsonProperty("prueba_escrita")]
        public bool WrittenTests { get; set; } = true;

        /// <summary>
        /// Cuántas entregas tiene su proyecto en cada semestre (D5a4), solo en los ramos sin
        /// pruebas escritas: 3 en un taller (avances y entrega final), 1 en un electivo, 2 por
        /// semestre en un seminario de tesis. Con 0 la nota sale como antes. Ver
        /// <see cref="Education.ProjectRules"/>.
        /// </summary>
        [JsonProperty("entregas")]
        public int Deliveries { get; set; }

        /// <summary>
        /// Si en este ramo se elige el área de la carrera (el electivo de especialidad, D5a4):
        /// la elección queda en la marca <c>electivo_&lt;área&gt;</c> y ayuda a que te llamen
        /// a entrevistas de esa área.
        /// </summary>
        [JsonProperty("elige_area")]
        public bool ChoosesArea { get; set; }

        /// <summary>Último semestre en que se tiene.</summary>
        public int LastSemester => ToSemester ?? FromSemester;

        /// <summary>Si se tiene en ese semestre de un plan de esa institución y ese largo.</summary>
        public bool In(string institution, int semester, int semesters)
        {
            if (Institutions.Count > 0 && !Institutions.Contains(institution))
            {
                return false;
            }

            return Last ? semester == semesters : semester >= FromSemester && semester <= LastSemester;
        }

        /// <summary>
        /// El número del ramo en ese semestre (1 para Programación I), o 0 si se tiene un
        /// solo semestre y no lleva número.
        /// </summary>
        public int Number(int semester) => Last || LastSemester == FromSemester ? 0 : semester - FromSemester + 1;
    }
}

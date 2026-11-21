using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Las preguntas de un ramo en un curso (archivos de preguntas/ del paquete del país, D5a):
    /// "Matemática de 5º básico", ordenadas en las unidades del programa oficial, en el orden en
    /// que se pasan en el año. Una tarde de estudio (<see cref="Education.StudyRules"/>) toma tres
    /// preguntas de la unidad que va.
    /// </summary>
    public sealed class QuestionSetDefinition
    {
        /// <summary>El ramo ("ramo.chile.matematica"): tiene que existir en la etapa.</summary>
        [JsonProperty("ramo", Required = Required.Always)]
        public string SubjectId { get; set; } = string.Empty;

        /// <summary>La etapa con curso ("etapa.chile.basica").</summary>
        [JsonProperty("etapa", Required = Required.Always)]
        public string StageId { get; set; } = string.Empty;

        /// <summary>El curso de la etapa, desde 1 ("5" es 5º básico).</summary>
        [JsonProperty("curso", Required = Required.Always)]
        public int Level { get; set; }

        /// <summary>De dónde sale (por ejemplo, "Programa de Estudio Matemática 5º básico, Mineduc 2012").</summary>
        [JsonProperty("fuente", Required = Required.Always)]
        public string Source { get; set; } = string.Empty;

        /// <summary>Las unidades del año, en orden.</summary>
        [JsonProperty("unidades", Required = Required.Always)]
        public List<QuestionUnitDefinition> Units { get; set; } = new List<QuestionUnitDefinition>();

        /// <summary>El paquete de donde viene (lo llena la base de contenido).</summary>
        [JsonIgnore]
        public string PackId { get; internal set; } = string.Empty;
    }
}

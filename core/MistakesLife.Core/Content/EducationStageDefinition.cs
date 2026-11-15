using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una etapa del sistema educativo de un país, por edad.
    /// Ejemplo en Chile: sala cuna, jardín, educación básica, educación media.
    /// </summary>
    public sealed class EducationStageDefinition
    {
        /// <summary>Por ejemplo "etapa.chile.basica".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Edad (años cumplidos) en que empieza.</summary>
        [JsonProperty("edad_desde", Required = Required.Always)]
        public int FromAge { get; set; }

        /// <summary>Edad (años cumplidos) en que ya no corresponde (excluida).</summary>
        [JsonProperty("edad_hasta", Required = Required.Always)]
        public int ToAge { get; set; }

        /// <summary>Si la ley la exige.</summary>
        [JsonProperty("obligatoria")]
        public bool Mandatory { get; set; }

        /// <summary>
        /// Si en esta etapa se va a un curso, con compañeros y profe jefe ("curso"):
        /// en Chile, básica y media.
        /// </summary>
        [JsonProperty("curso")]
        public bool HasClass { get; set; }

        /// <summary>Los ramos de la etapa, con los cursos en que se tienen ("ramos"). Solo en etapas con curso.</summary>
        [JsonProperty("ramos")]
        public System.Collections.Generic.List<SubjectDefinition> Subjects { get; set; } = new System.Collections.Generic.List<SubjectDefinition>();

        /// <summary>
        /// Las formas de pasar de curso ("promocion"); basta cumplir una. Sin ninguna, siempre
        /// se pasa de curso.
        /// </summary>
        [JsonProperty("promocion")]
        public System.Collections.Generic.List<PromotionRuleDefinition> Promotion { get; set; } = new System.Collections.Generic.List<PromotionRuleDefinition>();
    }
}

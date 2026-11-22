using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una prueba obligatoria de la admisión (D5a5), por ejemplo "matematica", con el ramo del
    /// colegio que mide ("ramo.chile.matematica"): lo que dominas de ese ramo es lo que sabes
    /// el día de la prueba.
    /// </summary>
    public sealed class AdmissionTestDefinition
    {
        /// <summary>Id corto de la prueba ("lectora", "matematica"); se usa en las ponderaciones.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>El ramo del colegio que mide.</summary>
        [JsonProperty("ramo", Required = Required.Always)]
        public string SubjectId { get; set; } = string.Empty;

        /// <summary>La clave del nombre de la prueba ("admision.prueba.lectora" = "Competencia Lectora").</summary>
        [JsonIgnore]
        public string TextKey => "admision.prueba." + Id;
    }
}

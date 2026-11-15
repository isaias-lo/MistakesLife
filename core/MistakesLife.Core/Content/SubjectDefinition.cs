using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un ramo de una etapa del colegio ("ramo.chile.matematica"), en los cursos en que se
    /// tiene (de "desde" a "hasta", contando desde 1). Su nombre va en el idioma del país.
    /// </summary>
    public sealed class SubjectDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Primer curso de la etapa en que se tiene (1 si falta).</summary>
        [JsonProperty("desde")]
        public int FromLevel { get; set; } = 1;

        /// <summary>Último curso de la etapa en que se tiene (todos si falta).</summary>
        [JsonProperty("hasta")]
        public int ToLevel { get; set; } = 99;

        /// <summary>
        /// Si tiene pruebas escritas que se estudian ("prueba_escrita", true si falta). En
        /// Educación Física o Música las notas llegan igual, pero no se anuncian como prueba.
        /// </summary>
        [JsonProperty("prueba_escrita")]
        public bool WrittenTests { get; set; } = true;

        /// <summary>Si se tiene en ese curso de la etapa.</summary>
        public bool InLevel(int level) => level >= FromLevel && level <= ToLevel;
    }
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una unidad del programa de un ramo ("tema.chile.matematica.5.fracciones"). Su id es la
    /// clave de su nombre en lang/ ("Fracciones y números mixtos") e <c>id.texto</c> la del texto
    /// breve que se lee antes de las preguntas.
    /// </summary>
    public sealed class QuestionUnitDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("preguntas", Required = Required.Always)]
        public List<QuestionDefinition> Questions { get; set; } = new List<QuestionDefinition>();

        /// <summary>La clave del texto breve que se lee antes de las preguntas.</summary>
        [JsonIgnore]
        public string ReadingKey => Id + ".texto";
    }
}

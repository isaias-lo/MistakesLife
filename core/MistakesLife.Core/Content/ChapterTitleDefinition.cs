using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// El título que le puede dar una línea del diario al año en que se escribió: "El año en
    /// que murió mi mamá", "El año en que me titulé". Cuando el año se cierra como capítulo,
    /// gana la línea de más peso (si empatan, la más nueva). El título puede nombrar a la
    /// persona de ese momento con {persona}.
    /// </summary>
    public sealed class ChapterTitleDefinition
    {
        /// <summary>Peso más alto permitido.</summary>
        public const int MaxWeight = 10;

        /// <summary>La clave de la línea del diario (<c>diario.muerte_madre</c>).</summary>
        [JsonProperty("diario", Required = Required.Always)]
        public string JournalKey { get; set; } = string.Empty;

        /// <summary>La clave del título (<c>capitulo.muerte_madre</c>).</summary>
        [JsonProperty("titulo", Required = Required.Always)]
        public string TitleKey { get; set; } = string.Empty;

        /// <summary>Cuánto marca el año, de 1 (algo que pasó) a 10 (algo que cambia la vida).</summary>
        [JsonProperty("peso", Required = Required.Always)]
        public int Weight { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;
    }
}

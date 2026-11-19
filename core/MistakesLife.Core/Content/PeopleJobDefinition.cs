using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Una pega de "trabajo_de_la_gente": una carrera u oficio del juego y la parte real de los ocupados que trabaja en ella.</summary>
    public sealed class PeopleJobDefinition
    {
        /// <summary>La carrera u oficio (por ejemplo "oficio.garzon"). Tiene que existir entera en el juego.</summary>
        [JsonProperty("carrera", Required = Required.Always)]
        public string CareerId { get; set; } = string.Empty;

        /// <summary>La parte de los ocupados que trabaja en ella (de 0 a 1).</summary>
        [JsonProperty("parte", Required = Required.Always)]
        public double Share { get; set; }

        /// <summary>
        /// La parte de quienes trabajan en ella que son mujeres (de 0 a 1), o null si no se
        /// sabe: entonces hombres y mujeres la tienen igual de seguido.
        /// </summary>
        [JsonProperty("mujeres")]
        public double? Women { get; set; }
    }
}

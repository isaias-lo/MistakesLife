using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>"estudia_superior" de "trabajo_de_la_gente": la parte de una edad que va a la educación superior.</summary>
    public sealed class PeopleStudyDefinition
    {
        [JsonProperty("desde", Required = Required.Always)]
        public int From { get; set; }

        [JsonProperty("hasta", Required = Required.Always)]
        public int To { get; set; }

        /// <summary>Tasa neta de asistencia: la parte de esa edad que estudia (de 0 a 1).</summary>
        [JsonProperty("parte", Required = Required.Always)]
        public double Share { get; set; }
    }
}

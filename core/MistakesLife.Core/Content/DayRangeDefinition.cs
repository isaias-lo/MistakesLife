using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tramo del año con fecha "día-mes" de inicio y de fin, ambos incluidos
    /// (por ejemplo las vacaciones de invierno: "desde": "23-06", "hasta": "04-07").
    /// </summary>
    public sealed class DayRangeDefinition
    {
        [JsonProperty("desde", Required = Required.Always)]
        public string From { get; set; } = string.Empty;

        [JsonProperty("hasta", Required = Required.Always)]
        public string To { get; set; } = string.Empty;

        /// <summary>El día del año (desde 0) de <see cref="From"/>; lo pone la base de contenido.</summary>
        [JsonIgnore]
        public int? FromDay { get; internal set; }

        /// <summary>El día del año (desde 0) de <see cref="To"/>; lo pone la base de contenido.</summary>
        [JsonIgnore]
        public int? ToDay { get; internal set; }
    }
}

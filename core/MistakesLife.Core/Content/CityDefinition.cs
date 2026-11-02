using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Una ciudad del país con sus comunas.</summary>
    public sealed class CityDefinition
    {
        /// <summary>Por ejemplo "ciudad.santiago".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("peso_nacimiento")]
        public int BirthWeight { get; set; } = 1;

        [JsonProperty("comunas")]
        public List<CommuneDefinition> Communes { get; set; } = new List<CommuneDefinition>();
    }
}

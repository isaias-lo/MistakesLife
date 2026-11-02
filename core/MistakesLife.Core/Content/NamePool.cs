using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Nombres y apellidos comunes de un país (nombres propios, no se traducen).</summary>
    public sealed class NamePool
    {
        [JsonProperty("femeninos")]
        public List<string> Female { get; set; } = new List<string>();

        [JsonProperty("masculinos")]
        public List<string> Male { get; set; } = new List<string>();

        [JsonProperty("apellidos")]
        public List<string> Surnames { get; set; } = new List<string>();
    }
}

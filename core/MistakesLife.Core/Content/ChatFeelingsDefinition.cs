using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Cuánto cambia lo que siente quien escribió por el jugador (cariño, confianza,
    /// respeto, rencor), por cómo le contestó o por dejarlo en visto. Ver
    /// <see cref="ChatDefinition"/>.
    /// </summary>
    public class ChatFeelingsDefinition
    {
        [JsonProperty("carino")]
        public int Affection { get; set; }

        [JsonProperty("confianza")]
        public int Trust { get; set; }

        [JsonProperty("respeto")]
        public int Respect { get; set; }

        [JsonProperty("rencor")]
        public int Resentment { get; set; }
    }
}

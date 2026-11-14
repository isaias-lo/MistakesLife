using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Cómo puede contestar una persona a lo que el jugador le escribió (ver
    /// <see cref="ChatOpenerDefinition"/>): su id es la clave del texto, y solo contesta así
    /// si siente lo que pide (carino_minimo, carino_maximo, rencor_minimo, rencor_maximo).
    /// Lo que cambia en ella (carino, confianza, respeto, rencor) pasa cuando contesta.
    /// </summary>
    public sealed class ChatAnswerDefinition : ChatFeelingsDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("carino_minimo")]
        public int MinAffection { get; set; }

        [JsonProperty("carino_maximo")]
        public int MaxAffection { get; set; } = Characters.Relationship.Max;

        [JsonProperty("rencor_minimo")]
        public int MinResentment { get; set; }

        [JsonProperty("rencor_maximo")]
        public int MaxResentment { get; set; } = Characters.Relationship.Max;

        /// <summary>Si alguien que siente esto contestaría así.</summary>
        public bool Fits(Characters.Relationship feelings) =>
            feelings.Affection >= MinAffection && feelings.Affection <= MaxAffection
            && feelings.Resentment >= MinResentment && feelings.Resentment <= MaxResentment;
    }
}

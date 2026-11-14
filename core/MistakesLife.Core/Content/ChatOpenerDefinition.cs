using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Algo que el jugador puede escribirle primero a alguien por Mensajito (archivos de
    /// escribir/ de un paquete), por ejemplo "escribir.amigo.juntarse". Su id es la clave del
    /// texto que manda. Solo se ofrece para la persona que ocupa ese lugar ("para") y siente lo
    /// que pide (carino_minimo... como en <see cref="ChatDefinition"/>), y si el jugador cumple
    /// las "condiciones". La semana siguiente esa persona contesta con la primera de sus
    /// "respuestas" que calce con lo que siente en ese momento; si ninguna calza, lo deja en visto.
    /// Ver <see cref="Devices.ChatSystem"/>.
    /// </summary>
    public sealed class ChatOpenerDefinition
    {
        /// <summary>Id estable: <c>escribir.&lt;para quien&gt;.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>A quién se le escribe: uno de <see cref="ChatDefinition.Senders"/>.</summary>
        [JsonProperty("para", Required = Required.Always)]
        public string To { get; set; } = string.Empty;

        [JsonProperty("carino_minimo")]
        public int MinAffection { get; set; }

        [JsonProperty("carino_maximo")]
        public int MaxAffection { get; set; } = Characters.Relationship.Max;

        [JsonProperty("rencor_minimo")]
        public int MinResentment { get; set; }

        [JsonProperty("rencor_maximo")]
        public int MaxResentment { get; set; } = Characters.Relationship.Max;

        /// <summary>Edad mínima de a quien se le escribe.</summary>
        [JsonProperty("edad_minima")]
        public int MinAge { get; set; }

        /// <summary>Edad máxima de a quien se le escribe.</summary>
        [JsonProperty("edad_maxima")]
        public int MaxAge { get; set; } = 150;

        /// <summary>Condición sobre el jugador, o vacía.</summary>
        [JsonProperty("condiciones")]
        public string Conditions { get; set; } = string.Empty;

        /// <summary>Cómo puede contestar la otra persona, en orden: contesta con la primera que calce.</summary>
        [JsonProperty("respuestas", Required = Required.Always)]
        public List<ChatAnswerDefinition> Answers { get; set; } = new List<ChatAnswerDefinition>();

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;
    }
}

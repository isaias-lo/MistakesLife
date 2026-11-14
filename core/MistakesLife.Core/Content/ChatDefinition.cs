using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un mensaje que una persona real de la vida del jugador le puede mandar por
    /// Mensajito (archivos de chats/ de un paquete), por ejemplo "chat.madre.llegaste".
    /// Su id es la clave del texto. Regla "nada se inventa": siempre lo escribe alguien
    /// que existe ("de": su mamá, su pareja, un amigo...) y que siente lo que pide
    /// (carino_minimo, carino_maximo, rencor_minimo, rencor_maximo; y su edad: edad_minima, edad_maxima), y llega por algo que pasó en la vida
    /// del jugador ("al_anotar": una línea de su diario) o, de vez en cuando, porque esa
    /// persona se acordó de él ("probabilidad" cada semana). Si tiene "respuestas", el
    /// jugador elige qué contestar; si no contesta en <see cref="Devices.ChatSystem.ReplyWeeks"/>
    /// semanas, lo dejó en visto ("si_no_respondes"). Ver <see cref="Devices.ChatSystem"/>.
    /// </summary>
    public sealed class ChatDefinition
    {
        /// <summary>Quién puede escribir: la persona con ese lugar en la vida del jugador.</summary>
        public static readonly string[] Senders = { "madre", "padre", "pareja", "hijo", "amigo", "colega", "rival", "ex" };

        /// <summary>Id estable: <c>chat.&lt;quien&gt;.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Uno de <see cref="Senders"/>.</summary>
        [JsonProperty("de", Required = Required.Always)]
        public string From { get; set; } = string.Empty;

        /// <summary>Líneas del diario del jugador que hacen que llegue este mensaje.</summary>
        [JsonProperty("al_anotar")]
        public List<string> OnJournal { get; set; } = new List<string>();

        /// <summary>
        /// Sin "al_anotar": la probabilidad (0 a 1) de que cada persona que cumple lo pedido
        /// escriba esto en una semana. Solo escribe si no está esperando que le contesten.
        /// </summary>
        [JsonProperty("probabilidad")]
        public double? Chance { get; set; }

        /// <summary>Lo mínimo de cariño que quien escribe siente por el jugador.</summary>
        [JsonProperty("carino_minimo")]
        public int MinAffection { get; set; }

        /// <summary>Lo máximo de cariño (para mensajes de alguien que se enfrió).</summary>
        [JsonProperty("carino_maximo")]
        public int MaxAffection { get; set; } = Characters.Relationship.Max;

        /// <summary>Lo mínimo de rencor (para el rival o el ex dolido).</summary>
        [JsonProperty("rencor_minimo")]
        public int MinResentment { get; set; }

        /// <summary>Lo máximo de rencor: alguien con mucho rencor no manda saludos.</summary>
        [JsonProperty("rencor_maximo")]
        public int MaxResentment { get; set; } = Characters.Relationship.Max;

        /// <summary>Edad mínima de quien escribe (por ejemplo, un hijo ya adulto). Nadie escribe antes de <see cref="Devices.ChatSystem.MinSenderAge"/>.</summary>
        [JsonProperty("edad_minima")]
        public int MinAge { get; set; }

        /// <summary>Edad máxima de quien escribe (por ejemplo, un hijo que todavía va al colegio).</summary>
        [JsonProperty("edad_maxima")]
        public int MaxAge { get; set; } = 150;

        /// <summary>Condición sobre el jugador (por ejemplo "jugador.edad < 18"), o vacía.</summary>
        [JsonProperty("condiciones")]
        public string Conditions { get; set; } = string.Empty;

        /// <summary>Lo que el jugador puede contestar. Vacío: no espera respuesta.</summary>
        [JsonProperty("respuestas")]
        public List<ChatReplyDefinition> Replies { get; set; } = new List<ChatReplyDefinition>();

        /// <summary>Lo que cambia en quien escribió si el jugador no contesta a tiempo.</summary>
        [JsonProperty("si_no_respondes")]
        public ChatFeelingsDefinition? IfIgnored { get; set; }

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;
    }
}

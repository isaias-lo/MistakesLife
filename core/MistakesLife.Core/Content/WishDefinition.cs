using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Algo que un personaje puede querer en una etapa de su vida, tal como se escribe en un
    /// paquete (carpeta deseos/). Su id es también la clave de su frase en lang/ ("Tener un
    /// amigo de verdad"), y "&lt;id&gt;.cumplido" y "&lt;id&gt;.perdido" son las líneas del diario.
    /// </summary>
    public sealed class WishDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("edad_minima")]
        public int MinAge { get; set; }

        [JsonProperty("edad_maxima")]
        public int MaxAge { get; set; } = 150;

        /// <summary>Cuándo puede nacer este deseo (lenguaje de condiciones): de qué vida sale.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>Cuándo se cumple (lenguaje de condiciones). Obligatorio.</summary>
        [JsonProperty("cumplido", Required = Required.Always)]
        public string Fulfilled { get; set; } = string.Empty;

        /// <summary>
        /// Con quién es el deseo (madre, padre, pareja, hijo, amigo, colega, rival, ex): se
        /// elige a alguien de verdad, que en las condiciones es "persona" y en la frase {persona}.
        /// </summary>
        [JsonProperty("con")]
        public string? With { get; set; }

        /// <summary>
        /// La actividad del horario que ayuda a cumplirlo: si el jugador lo persigue, el
        /// horario sugerido la pone más seguido. Opcional.
        /// </summary>
        [JsonProperty("actividad")]
        public string? Activity { get; set; }

        /// <summary>Qué tan probable es frente a otros deseos posibles.</summary>
        [JsonProperty("peso")]
        public int Weight { get; set; } = 10;

        /// <summary>Si se puede volver a querer en otra etapa después de cumplirlo (por defecto no).</summary>
        [JsonProperty("repetible")]
        public bool Repeatable { get; set; }

        [JsonIgnore]
        public string FulfilledJournalKey => Id + ".cumplido";

        [JsonIgnore]
        public string LostJournalKey => Id + ".perdido";

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public Expression? CompiledFulfilled { get; set; }
    }
}

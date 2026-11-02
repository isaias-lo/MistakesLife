using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Una opción de un evento: lo que puede elegir el jugador.</summary>
    public sealed class EventOptionDefinition
    {
        /// <summary>Clave del texto que ve el jugador, por ejemplo "opcion.reconocer_error".</summary>
        [JsonProperty("texto", Required = Required.Always)]
        public string TextKey { get; set; } = string.Empty;

        /// <summary>Si no se cumple, la opción no aparece. Opcional.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>
        /// Qué tan atractiva es esta opción para el personaje, como un número (0 o más).
        /// La usan los personajes que deciden solos (el simulador y, más adelante, los
        /// personajes del mundo). Por ejemplo "jugador.responsabilidad". Si falta, vale 50.
        /// </summary>
        [JsonProperty("tendencia")]
        public string? Tendency { get; set; }

        [JsonProperty("efectos")]
        public List<string> Effects { get; set; } = new List<string>();

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public Expression? CompiledTendency { get; set; }

        [JsonIgnore]
        public List<Effect> CompiledEffects { get; } = new List<Effect>();
    }
}

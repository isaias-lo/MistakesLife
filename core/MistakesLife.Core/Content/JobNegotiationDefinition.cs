using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "negociacion" de economia.busqueda_trabajo: qué pasa al pedir más sueldo
    /// ante una oferta. Lo que no es mejora ni retiro es "te mantienen la oferta".
    /// </summary>
    public sealed class JobNegotiationDefinition
    {
        /// <summary>Cuánto sube el sueldo si aceptan (0.08 = 8 % más).</summary>
        [JsonProperty("mejora")]
        public double Raise { get; set; } = 0.08;

        /// <summary>Probabilidad de que acepten subir el sueldo.</summary>
        [JsonProperty("prob_mejora")]
        public double RaiseChance { get; set; } = 0.4;

        /// <summary>Probabilidad de que retiren la oferta y tenga que seguir buscando.</summary>
        [JsonProperty("prob_retiro")]
        public double WithdrawChance { get; set; } = 0.1;
    }
}

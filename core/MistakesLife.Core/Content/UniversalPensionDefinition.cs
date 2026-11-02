using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Pensión del Estado para personas mayores (en Chile, la PGU). Es completa si
    /// la pensión propia es baja y baja poco a poco hasta cero si es alta.
    /// Montos mensuales en dinero del inicio del mundo.
    /// </summary>
    public sealed class UniversalPensionDefinition
    {
        /// <summary>Monto completo al mes.</summary>
        [JsonProperty("monto", Required = Required.Always)]
        public decimal Amount { get; set; }

        /// <summary>Con una pensión propia hasta este monto, se recibe completa.</summary>
        [JsonProperty("completa_hasta")]
        public decimal? FullUpTo { get; set; }

        /// <summary>Con una pensión propia desde este monto, ya no se recibe.</summary>
        [JsonProperty("sin_pension_desde")]
        public decimal? NoneFrom { get; set; }
    }
}

using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.jubilacion" de country.json: cómo se financia la vejez en
    /// ese país. Cada parte es opcional, así cada país arma su propio sistema
    /// (Chile: cuenta individual en una AFP + Pensión Garantizada Universal;
    /// Argentina: reparto en la ANSES + PUAM).
    /// </summary>
    public sealed class PensionsDefinition
    {
        /// <summary>Ahorro obligatorio en una cuenta propia (capitalización individual). Si falta, no hay cuenta.</summary>
        [JsonProperty("cuenta_individual")]
        public PensionAccountDefinition? IndividualAccount { get; set; }

        /// <summary>Jubilación de reparto: el Estado paga según los años de aportes (Argentina). Si falta, no hay.</summary>
        [JsonProperty("reparto")]
        public PayAsYouGoPensionDefinition? PayAsYouGo { get; set; }

        /// <summary>Pensión que paga el Estado a las personas mayores. Si falta, no hay.</summary>
        [JsonProperty("pension_universal")]
        public UniversalPensionDefinition? UniversalPension { get; set; }
    }
}

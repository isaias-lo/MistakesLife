using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.liquidacion" de country.json: cómo se pasa del sueldo bruto
    /// al líquido en el país, según el tipo de trabajo.
    /// </summary>
    public sealed class PayrollDefinition
    {
        /// <summary>Descuentos de quien tiene contrato (trabajador dependiente).</summary>
        [JsonProperty("dependiente")]
        public List<DeductionDefinition> Employee { get; set; } = new List<DeductionDefinition>();

        /// <summary>Descuentos de quien trabaja a honorarios (boletas).</summary>
        [JsonProperty("honorarios")]
        public List<DeductionDefinition> Freelance { get; set; } = new List<DeductionDefinition>();

        /// <summary>
        /// Tramos del impuesto mensual que se descuenta a quien tiene contrato (en
        /// Chile, el impuesto único de segunda categoría), de menor a mayor.
        /// </summary>
        [JsonProperty("impuesto_mensual")]
        public List<TaxBracketDefinition> MonthlyTax { get; set; } = new List<TaxBracketDefinition>();
    }
}

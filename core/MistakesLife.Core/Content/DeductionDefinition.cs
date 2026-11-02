using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un descuento de la liquidación de sueldo, por ejemplo la cotización de la AFP.
    /// Su id (que empieza con "descuento.") es también la clave de su nombre en lang/.
    /// </summary>
    public sealed class DeductionDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Parte del sueldo bruto que se descuenta (0.07 = 7 %).</summary>
        [JsonProperty("tasa", Required = Required.Always)]
        public double Rate { get; set; }

        /// <summary>
        /// Sueldo máximo sobre el que se calcula (el "tope imponible"), en dinero del
        /// inicio del mundo. Si falta, no hay tope.
        /// </summary>
        [JsonProperty("tope")]
        public decimal? Cap { get; set; }

        /// <summary>Si se resta del sueldo antes de calcular el impuesto (como las cotizaciones obligatorias).</summary>
        [JsonProperty("rebaja_impuesto")]
        public bool ReducesTax { get; set; }
    }
}

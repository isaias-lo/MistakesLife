using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tramo del impuesto a la renta: la parte del ingreso desde "desde" (y
    /// hasta el tramo siguiente) paga "tasa". Montos en dinero de los paquetes.
    /// </summary>
    public sealed class TaxBracketDefinition
    {
        [JsonProperty("desde", Required = Required.Always)]
        public decimal From { get; set; }

        [JsonProperty("tasa", Required = Required.Always)]
        public double Rate { get; set; }
    }
}

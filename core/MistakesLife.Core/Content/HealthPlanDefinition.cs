using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un plan de salud privado (una isapre en Chile), por ejemplo
    /// "prevision.chile.isapre.preferente". Su id es también la clave de su nombre en lang/.
    /// </summary>
    public sealed class HealthPlanDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Precio base al mes, en dinero de los paquetes; se multiplica por el factor de la edad.</summary>
        [JsonProperty("precio_base", Required = Required.Always)]
        public decimal BasePrice { get; set; }

        /// <summary>Parte del precio de una atención que paga el plan (0.7 = 70 %); el resto es el copago.</summary>
        [JsonProperty("cobertura", Required = Required.Always)]
        public double Coverage { get; set; }
    }
}

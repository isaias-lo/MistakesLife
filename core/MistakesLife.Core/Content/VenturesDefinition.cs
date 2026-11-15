using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.negocios" de country.json: cómo le va a un negocio chico entre socios
    /// (un microemprendimiento). Las ganancias se dan en sueldos mínimos por mes, para el
    /// negocio entero; el cierre, como la probabilidad real de que una empresa nueva cierre. Cada socio gana lo que gana un microemprendedor típico.
    /// </summary>
    public sealed class VenturesDefinition
    {
        /// <summary>Ganancia típica de un mes para cada socio de un negocio formal (con empresa y boletas), en sueldos mínimos.</summary>
        [JsonProperty("ganancia_formal", Required = Required.Always)]
        public double FormalProfit { get; set; }

        /// <summary>Ganancia típica de un mes para cada socio de un negocio informal (de palabra, sin empresa), en sueldos mínimos.</summary>
        [JsonProperty("ganancia_informal", Required = Required.Always)]
        public double InformalProfit { get; set; }

        /// <summary>
        /// Qué tan distinto es un negocio de otro (la desviación del logaritmo de la ganancia): la
        /// mayoría gana menos que el promedio y unos pocos ganan mucho más. Se elige al abrir.
        /// </summary>
        [JsonProperty("variacion")]
        public double Spread { get; set; } = 0.8;

        /// <summary>Probabilidad de cerrar en el primer año.</summary>
        [JsonProperty("cierre_primer_ano", Required = Required.Always)]
        public double FirstYearClosure { get; set; }

        /// <summary>Probabilidad de cerrar en cada año siguiente.</summary>
        [JsonProperty("cierre_anual", Required = Required.Always)]
        public double YearlyClosure { get; set; }

        /// <summary>
        /// Impuesto que se descuenta de la ganancia formal (0,25 = 25 %), si la ganancia del país
        /// está medida antes de impuestos. Opcional: 0 si ya es lo que le queda a cada uno.
        /// </summary>
        [JsonProperty("impuesto_formal")]
        public double FormalTax { get; set; }
    }
}

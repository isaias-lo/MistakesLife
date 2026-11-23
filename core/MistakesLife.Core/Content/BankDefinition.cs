using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un banco de "economia.bancos": qué cuenta da, cuánto cobra al mes y qué pide para abrirla.
    /// Los montos van en dinero de los paquetes (se ajustan con la inflación).
    /// </summary>
    public sealed class BankDefinition
    {
        /// <summary>Id del banco (banco.chile.nacional). Su nombre va en la tabla de idioma.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = "";

        /// <summary>"estatal", "grande" o "digital": cómo se presenta en la lista.</summary>
        [JsonProperty("tipo", Required = Required.Always)]
        public string Kind { get; set; } = "";

        /// <summary>Clave de la cuenta que da (cuenta.vista, cuenta.corriente).</summary>
        [JsonProperty("cuenta", Required = Required.Always)]
        public string AccountKind { get; set; } = "";

        /// <summary>Lo que cobra al mes por mantener la cuenta (0 si no cobra).</summary>
        [JsonProperty("mantencion")]
        public decimal MonthlyFee { get; set; }

        /// <summary>Sueldo líquido mínimo que pide para abrirla (0 si no pide).</summary>
        [JsonProperty("renta_minima")]
        public decimal MinIncome { get; set; }

        /// <summary>Si pide un trabajo con contrato para abrirla.</summary>
        [JsonProperty("pide_contrato")]
        public bool NeedsContract { get; set; }
    }
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.declaracion_anual" de country.json: la declaración anual
    /// de impuestos (en Chile, la Operación Renta de abril). Todos los montos en
    /// dinero del inicio del mundo.
    /// </summary>
    public sealed class AnnualTaxDefinition
    {
        /// <summary>Mes (1 a 12) en que se declara lo ganado el año anterior. Hay plazo hasta que empieza el mes siguiente.</summary>
        [JsonProperty("mes", Required = Required.Always)]
        public int Month { get; set; }

        /// <summary>Tramos del impuesto anual, de menor a mayor (en Chile, el global complementario).</summary>
        [JsonProperty("tramos", Required = Required.Always)]
        public List<TaxBracketDefinition> Brackets { get; set; } = new List<TaxBracketDefinition>();

        /// <summary>Parte de los honorarios que se descuenta como gastos sin tener que probarlos (0.3 = 30 %).</summary>
        [JsonProperty("gastos_presuntos_honorarios")]
        public double FreelancePresumedExpenses { get; set; }

        /// <summary>Máximo de esos gastos presuntos al año. Si falta, sin máximo.</summary>
        [JsonProperty("tope_gastos_presuntos")]
        public decimal? FreelancePresumedExpensesCap { get; set; }

        /// <summary>Parte de los honorarios sobre la que se cotiza (0.8 = 80 %).</summary>
        [JsonProperty("base_cotizacion_honorarios")]
        public double FreelanceContributionBase { get; set; }

        /// <summary>Cotizaciones de previsión y salud que paga quien trabaja a honorarios, sobre esa base.</summary>
        [JsonProperty("tasa_cotizacion_honorarios")]
        public double FreelanceContributionRate { get; set; }

        /// <summary>Multa si no se declara a tiempo y había que pagar (0.1 = 10 % más de lo que se debía).</summary>
        [JsonProperty("multa_atraso")]
        public double LatePenalty { get; set; }
    }
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.seguro_desempleo" de country.json: el seguro que paga cuando
    /// alguien con contrato pierde su trabajo. El núcleo solo sabe que existe un
    /// seguro así; cómo se llama y sus reglas vienen del paquete de cada país (en
    /// Chile, el Seguro de Cesantía de la AFC, ley 19.728).
    /// - Cada mes con sueldo con contrato, el descuento del trabajador y el aporte del
    ///   empleador se ahorran en una cuenta propia (la Cuenta Individual de Cesantía).
    /// - Al quedar sin trabajo, con las cotizaciones mínimas, la cuenta paga sola cada
    ///   mes un porcentaje del sueldo promedio, que baja mes a mes, mientras le quede saldo.
    /// - Si el saldo no alcanza y el despido no fue culpa suya, completa un fondo común
    ///   (el Fondo de Cesantía Solidario) por algunos meses, con un mínimo y un máximo.
    /// </summary>
    public sealed class UnemploymentInsuranceDefinition
    {
        /// <summary>Id del descuento de la liquidación con contrato que va a la cuenta (por ejemplo "descuento.chile.seguro_cesantia").</summary>
        [JsonProperty("descuento", Required = Required.Always)]
        public string Deduction { get; set; } = string.Empty;

        /// <summary>
        /// Lo que aporta el empleador a la cuenta, como parte del mismo sueldo con el que
        /// se calcula el descuento (en Chile, 1,6 % con contrato indefinido). Lo que el
        /// empleador pone en el fondo común no se guarda: es de todos.
        /// </summary>
        [JsonProperty("aporte_empleador_cuenta")]
        public double EmployerRate { get; set; }

        /// <summary>Cotizaciones mensuales que se piden para cobrar, contadas desde el último cobro.</summary>
        [JsonProperty("cotizaciones_minimas", Required = Required.Always)]
        public int MinimumContributions { get; set; }

        /// <summary>De cuántos meses de sueldo sale el sueldo promedio con que se calculan los pagos.</summary>
        [JsonProperty("meses_promedio", Required = Required.Always)]
        public int AverageMonths { get; set; }

        /// <summary>
        /// Parte del sueldo promedio que se paga cada mes (0.7 = 70 %), en orden. Con la
        /// cuenta propia, después del último se sigue pagando el último mientras quede saldo.
        /// </summary>
        [JsonProperty("pagos", Required = Required.Always)]
        public List<double> Payments { get; set; } = new List<double>();

        /// <summary>El fondo común que completa cuando la cuenta no alcanza, o null si el país no tiene.</summary>
        [JsonProperty("fondo_solidario")]
        public SolidarityFundDefinition? SolidarityFund { get; set; }

        /// <summary>Clave del diario cuando llega el primer pago (el correo del seguro la escucha).</summary>
        [JsonProperty("diario_primer_pago", Required = Required.Always)]
        public string FirstPaymentJournal { get; set; } = string.Empty;

        /// <summary>Clave del diario cuando llega el último pago.</summary>
        [JsonProperty("diario_ultimo_pago", Required = Required.Always)]
        public string LastPaymentJournal { get; set; } = string.Empty;

        /// <summary>Clave del diario cuando pierde el trabajo y no le alcanzan las cotizaciones para cobrar.</summary>
        [JsonProperty("diario_sin_derecho", Required = Required.Always)]
        public string NoRightJournal { get; set; } = string.Empty;

        /// <summary>Clave del diario cuando se jubila y retira de una vez lo que le quedaba en la cuenta.</summary>
        [JsonProperty("diario_retiro_al_jubilar", Required = Required.Always)]
        public string RetirementJournal { get; set; } = string.Empty;
    }
}

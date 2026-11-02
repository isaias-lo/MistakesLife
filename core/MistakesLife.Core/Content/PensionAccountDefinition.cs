using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// La cuenta individual de pensión (en Chile, la cuenta en la AFP): cada mes
    /// se ahorra parte del sueldo, el fondo gana una rentabilidad y al jubilarse
    /// se convierte en una pensión mensual para el resto de la vida.
    /// </summary>
    public sealed class PensionAccountDefinition
    {
        /// <summary>
        /// Id del descuento de la liquidación de quien tiene contrato que alimenta
        /// la cuenta (por ejemplo "descuento.chile.afp").
        /// </summary>
        [JsonProperty("descuento", Required = Required.Always)]
        public string Deduction { get; set; } = string.Empty;

        /// <summary>
        /// Parte de ese descuento que llega a la cuenta; el resto es comisión y
        /// seguro (en Chile, 10 de cada 11,15 puntos).
        /// </summary>
        [JsonProperty("parte_a_la_cuenta")]
        public double ShareToAccount { get; set; } = 1;

        /// <summary>
        /// Quien trabaja a honorarios: parte de la base de cotización (la de
        /// "declaracion_anual") que llega a la cuenta cuando declara. 0 si no cotiza.
        /// </summary>
        [JsonProperty("tasa_honorarios")]
        public double FreelanceRate { get; set; }

        /// <summary>Rentabilidad anual del fondo, por sobre la inflación (0.035 = 3,5 %).</summary>
        [JsonProperty("rentabilidad_real_anual")]
        public double RealAnnualReturn { get; set; }

        /// <summary>
        /// Años que debe durar la pensión al jubilarse (según la esperanza de vida);
        /// con eso el fondo se reparte en una pensión fija mensual.
        /// </summary>
        [JsonProperty("anos_de_pension", Required = Required.Always)]
        public int PensionYears { get; set; }
    }
}

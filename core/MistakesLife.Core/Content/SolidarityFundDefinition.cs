using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// El fondo común del seguro de desempleo (en Chile, el Fondo de Cesantía
    /// Solidario): completa los pagos cuando la cuenta propia no alcanza, solo para
    /// quien perdió el trabajo sin culpa suya y llevaba unos meses seguidos con el
    /// mismo empleador. Sus pagos tienen un mínimo y un máximo.
    /// </summary>
    public sealed class SolidarityFundDefinition
    {
        /// <summary>
        /// Las marcas del jugador que dicen que el despido no fue culpa suya (por
        /// ejemplo "despedido_necesidades" o "despedido_quiebra"). Sin una de ellas, no hay fondo.
        /// </summary>
        [JsonProperty("marcas", Required = Required.Always)]
        public List<string> Flags { get; set; } = new List<string>();

        /// <summary>Meses seguidos que debió llevar con su último empleador.</summary>
        [JsonProperty("meses_seguidos_ultimo_empleador")]
        public int MonthsWithLastEmployer { get; set; }

        /// <summary>Cuántos pagos puede completar el fondo (los primeros de la lista "pagos").</summary>
        [JsonProperty("pagos", Required = Required.Always)]
        public int Payments { get; set; }

        /// <summary>
        /// Lo mínimo y lo máximo del primer pago, en dinero de los paquetes. Los pagos
        /// siguientes bajan en la misma proporción que su porcentaje.
        /// </summary>
        [JsonProperty("minimo_primer_pago", Required = Required.Always)]
        public decimal FirstMinimum { get; set; }

        /// <summary>Lo máximo del primer pago, en dinero de los paquetes.</summary>
        [JsonProperty("maximo_primer_pago", Required = Required.Always)]
        public decimal FirstMaximum { get; set; }
    }
}

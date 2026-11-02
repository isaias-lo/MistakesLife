using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un régimen de inflación de un país, por ejemplo "economia.inflacion.estable".
    /// Mientras dura, la inflación anual se mueve un poco cada mes dentro de su rango.
    /// </summary>
    public sealed class InflationRegimeDefinition
    {
        /// <summary>Empieza con "economia.inflacion.".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Inflación anual mínima del rango (0.02 = 2 % al año).</summary>
        [JsonProperty("anual_min", Required = Required.Always)]
        public double AnnualMin { get; set; }

        /// <summary>Inflación anual máxima del rango.</summary>
        [JsonProperty("anual_max", Required = Required.Always)]
        public double AnnualMax { get; set; }

        /// <summary>
        /// Cuánto puede subir o bajar la inflación anual de un mes a otro
        /// (0.003 = 0,3 puntos). Con 0 queda fija en el valor que salió al empezar.
        /// </summary>
        [JsonProperty("variacion_mensual")]
        public double MonthlyVariation { get; set; }

        /// <summary>
        /// Probabilidad al año de pasar a otro régimen: id del régimen → probabilidad
        /// (0 a 1). Por ejemplo, { "economia.inflacion.alta": 0.08 } es, en promedio,
        /// un episodio de inflación alta cada 12 años. Las que faltan valen 0: a ese
        /// régimen solo se llega con un evento (cambiar_inflacion).
        /// </summary>
        [JsonProperty("cambio_anual")]
        public Dictionary<string, double> AnnualSwitch { get; set; } = new Dictionary<string, double>();
    }
}

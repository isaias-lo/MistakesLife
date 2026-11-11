using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una fase del ciclo económico, por ejemplo "economia.ciclo.crisis". Al
    /// empezar, el jugador lo lee en su diario ("diario." + id), la inflación puede
    /// cambiar de régimen y los fondos de pensión pueden caer o subir.
    /// </summary>
    public sealed class EconomicPhaseDefinition
    {
        /// <summary>Empieza con "economia.ciclo.".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Probabilidad al año de pasar a otra fase: id de la fase → probabilidad (0 a 1),
        /// igual que los regímenes de inflación.
        /// </summary>
        [JsonProperty("cambio_anual")]
        public Dictionary<string, double> AnnualSwitch { get; set; } = new Dictionary<string, double>();

        /// <summary>Régimen de inflación al que pasa el país al empezar esta fase. Si falta, no cambia.</summary>
        [JsonProperty("inflacion")]
        public string? InflationRegime { get; set; }

        /// <summary>
        /// Cuánto cambian de una vez los fondos de pensión de quienes viven en el país
        /// al empezar esta fase (-0.2 = caen 20 %). Si falta, no cambian.
        /// </summary>
        [JsonProperty("golpe_fondos")]
        public double PensionFundShock { get; set; }

        /// <summary>
        /// Cuántas veces más (o menos) quiebran las empresas del país durante esta
        /// fase, comparado con un año normal (2 = el doble). Si falta, 1.
        /// </summary>
        [JsonProperty("quiebras")]
        public double BankruptcyFactor { get; set; } = 1;

        /// <summary>
        /// Cuántas veces más (o menos) llaman a entrevistas durante esta fase,
        /// comparado con un año normal (0.6 = 40 % menos). Si falta, 1.
        /// </summary>
        [JsonProperty("contratacion")]
        public double HiringFactor { get; set; } = 1;
    }
}

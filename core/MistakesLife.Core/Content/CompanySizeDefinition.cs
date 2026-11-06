using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Un tamaño de empresa del país, por ejemplo "empresa.tamano.micro".</summary>
    public sealed class CompanySizeDefinition
    {
        /// <summary>Empieza con "empresa.tamano.". También es la clave de su texto.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Parte de los trabajadores con contrato que trabaja en empresas de este tamaño (los pesos se reparten entre todos).</summary>
        [JsonProperty("peso", Required = Required.Always)]
        public double Weight { get; set; }

        /// <summary>Cuánto paga comparado con lo típico (1 = lo típico; 0,8 = 20 % menos).</summary>
        [JsonProperty("sueldo_relativo")]
        public double SalaryRelative { get; set; } = 1;

        /// <summary>
        /// El cargo más alto que existe en una empresa de este tamaño, contado desde
        /// el cargo inicial (0 = solo el cargo inicial, 2 = dos ascensos). Si falta, no
        /// hay tope: están todos los cargos.
        /// </summary>
        [JsonProperty("cargo_maximo")]
        public int? MaxPositionDepth { get; set; }

        /// <summary>Probabilidad de quebrar en un año normal (las fases del ciclo la multiplican con "quiebras").</summary>
        [JsonProperty("quiebra_anual")]
        public double AnnualBankruptcy { get; set; }

        /// <summary>Probabilidad al año de crecer al tamaño siguiente.</summary>
        [JsonProperty("crece_anual")]
        public double AnnualGrowth { get; set; }

        /// <summary>Probabilidad al año de achicarse al tamaño anterior.</summary>
        [JsonProperty("achica_anual")]
        public double AnnualShrink { get; set; }
    }
}

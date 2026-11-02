using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una fila de la tabla de mortalidad de un país (campo "mortalidad" de
    /// country.json): la probabilidad de morir durante un año a una edad dada,
    /// por sexo. Sale de las tablas de vida oficiales (en Chile, del INE).
    /// </summary>
    public sealed class MortalityRateDefinition
    {
        /// <summary>Edad (años cumplidos) de esta fila.</summary>
        [JsonProperty("edad", Required = Required.Always)]
        public int Age { get; set; }

        /// <summary>Probabilidad de morir en un año para una mujer de esa edad (0 a 1).</summary>
        [JsonProperty("femenino", Required = Required.Always)]
        public double Female { get; set; }

        /// <summary>Probabilidad de morir en un año para un hombre de esa edad (0 a 1).</summary>
        [JsonProperty("masculino", Required = Required.Always)]
        public double Male { get; set; }
    }
}

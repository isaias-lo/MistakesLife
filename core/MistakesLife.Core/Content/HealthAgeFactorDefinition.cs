using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Una fila de la tabla de factores de los planes privados: desde esa edad, el precio base se multiplica por el factor.</summary>
    public sealed class HealthAgeFactorDefinition
    {
        [JsonProperty("desde", Required = Required.Always)]
        public int FromAge { get; set; }

        [JsonProperty("factor", Required = Required.Always)]
        public double Factor { get; set; }
    }
}

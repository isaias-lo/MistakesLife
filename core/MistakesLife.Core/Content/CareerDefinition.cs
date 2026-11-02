using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una carrera (archivo career.json de un paquete de carrera): su id y sus niveles
    /// de estudio. Lo que pasa mientras se estudia o trabaja va en los eventos del paquete.
    /// </summary>
    public sealed class CareerDefinition
    {
        /// <summary>Por ejemplo "carrera.contabilidad". Es también la clave de su nombre.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("niveles", Required = Required.Always)]
        public List<CareerLevelDefinition> Levels { get; set; } = new List<CareerLevelDefinition>();

        [JsonIgnore]
        public string PackId { get; internal set; } = string.Empty;
    }
}

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// La inflación de un país, generada por el juego (no hay años reales).
    /// El país pasa por "regímenes" (estable, alta, hiperinflación…): cada uno
    /// tiene su rango de inflación anual y su probabilidad de cambiar a otro.
    /// </summary>
    public sealed class InflationDefinition
    {
        /// <summary>Régimen con que empieza el mundo.</summary>
        [JsonProperty("regimen_inicial", Required = Required.Always)]
        public string InitialRegime { get; set; } = string.Empty;

        [JsonProperty("regimenes", Required = Required.Always)]
        public List<InflationRegimeDefinition> Regimes { get; set; } = new List<InflationRegimeDefinition>();

        /// <summary>El régimen con ese id, o null.</summary>
        public InflationRegimeDefinition? Regime(string id) => Regimes.FirstOrDefault(r => r.Id == id);
    }
}

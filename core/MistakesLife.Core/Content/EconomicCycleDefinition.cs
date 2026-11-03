using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.ciclo" de country.json: el ciclo económico del país
    /// (normal, recesión, crisis…), generado por el juego sin años reales. Como la
    /// inflación, pasa de una fase a otra con probabilidades del paquete, y los
    /// eventos pueden provocar una con cambiar_ciclo().
    /// </summary>
    public sealed class EconomicCycleDefinition
    {
        /// <summary>Fase con que empieza el mundo.</summary>
        [JsonProperty("fase_inicial", Required = Required.Always)]
        public string InitialPhase { get; set; } = string.Empty;

        [JsonProperty("fases", Required = Required.Always)]
        public List<EconomicPhaseDefinition> Phases { get; set; } = new List<EconomicPhaseDefinition>();

        /// <summary>La fase con ese id, o null.</summary>
        public EconomicPhaseDefinition? Phase(string id) => Phases.FirstOrDefault(p => p.Id == id);
    }
}

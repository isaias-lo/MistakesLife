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

        /// <summary>
        /// Índice de precios con que empieza el mundo (cuando nace el jugador). Los
        /// montos de los paquetes están a precios de hoy (índice 1); con 0.42, al nacer
        /// todo cuesta el 42 % de eso y, con la inflación del país, vuelve a precios de
        /// hoy cerca de los 25 años. Si falta, 1: el mundo empieza con los precios de los paquetes.
        /// </summary>
        [JsonProperty("indice_inicial")]
        public double InitialIndex { get; set; } = 1;

        [JsonProperty("regimenes", Required = Required.Always)]
        public List<InflationRegimeDefinition> Regimes { get; set; } = new List<InflationRegimeDefinition>();

        /// <summary>El régimen con ese id, o null.</summary>
        public InflationRegimeDefinition? Regime(string id) => Regimes.FirstOrDefault(r => r.Id == id);
    }
}

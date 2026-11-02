using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia" de country.json: cómo funciona el dinero en ese país.
    /// Por ahora, la inflación; después se suman sueldos, gastos, deudas y pensiones.
    /// </summary>
    public sealed class EconomyDefinition
    {
        /// <summary>Cómo genera el juego la inflación del país.</summary>
        [JsonProperty("inflacion", Required = Required.Always)]
        public InflationDefinition Inflation { get; set; } = new InflationDefinition();
    }
}

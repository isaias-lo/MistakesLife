using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.vivienda" de country.json: cómo se arrienda en el país.
    /// El jugador elige qué arrendar (jugador.arrendar('vivienda.pieza')) y firma
    /// un contrato con el precio de su comuna; el arriendo de cada mes sale de ese
    /// contrato, no de un monto fijo.
    /// </summary>
    public sealed class HousingDefinition
    {
        /// <summary>Meses que dura un contrato antes de renovarlo o irse (en Chile, 12).</summary>
        [JsonProperty("plazo_meses")]
        public int TermMonths { get; set; } = 12;

        /// <summary>Meses de arriendo que se dejan de garantía al firmar; se devuelven al irse.</summary>
        [JsonProperty("garantia_meses")]
        public int DepositMonths { get; set; } = 1;

        /// <summary>Cada cuántos meses el arriendo sube con la inflación (en Chile, una vez al año con el IPC).</summary>
        [JsonProperty("reajuste_meses")]
        public int AdjustEveryMonths { get; set; } = 12;

        [JsonProperty("tipos", Required = Required.Always)]
        public List<HousingTypeDefinition> Types { get; set; } = new List<HousingTypeDefinition>();

        /// <summary>El tipo de vivienda con ese id, o null si el país no lo tiene.</summary>
        public HousingTypeDefinition? Type(string id) => Types.FirstOrDefault(t => t.Id == id);
    }
}

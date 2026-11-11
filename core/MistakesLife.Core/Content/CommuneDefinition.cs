using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Una comuna o barrio dentro de una ciudad.</summary>
    public sealed class CommuneDefinition
    {
        /// <summary>Por ejemplo "comuna.puente_alto".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Qué tan probable es nacer aquí (por ejemplo, su población).</summary>
        [JsonProperty("peso_nacimiento")]
        public int BirthWeight { get; set; } = 1;

        /// <summary>Nivel socioeconómico mínimo de un hogar de esta comuna (0 a 100).</summary>
        [JsonProperty("nivel_hogar_min")]
        public int HouseholdLevelMin { get; set; }

        /// <summary>Nivel socioeconómico máximo de un hogar de esta comuna (0 a 100).</summary>
        [JsonProperty("nivel_hogar_max")]
        public int HouseholdLevelMax { get; set; } = 100;

        /// <summary>
        /// Arriendo mensual típico de cada tipo de vivienda en la comuna (por id de
        /// "economia.vivienda.tipos"), en dinero del inicio del mundo. Es el precio de
        /// un contrato nuevo; lo lee la función arriendo('vivienda.pieza').
        /// </summary>
        [JsonProperty("arriendos")]
        public Dictionary<string, decimal> Rents { get; set; } = new Dictionary<string, decimal>();

        /// <summary>El arriendo típico de ese tipo de vivienda aquí, o null si la comuna no lo tiene.</summary>
        public decimal? RentOf(string typeId) => Rents.TryGetValue(typeId, out decimal rent) ? rent : (decimal?)null;
    }
}

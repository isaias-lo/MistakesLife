using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tipo de vivienda que se puede arrendar en el país, por ejemplo
    /// "vivienda.pieza", "vivienda.departamento" o "vivienda.casa". Su precio
    /// está en cada comuna ("arriendos").
    /// </summary>
    public sealed class HousingTypeDefinition
    {
        /// <summary>Id que empieza con "vivienda.".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Si el arriendo ya incluye la luz, el agua y el internet (como pasa casi
        /// siempre en una pieza). Los eventos lo leen con jugador.cuentas_incluidas.
        /// </summary>
        [JsonProperty("incluye_cuentas")]
        public bool IncludesBills { get; set; }
    }
}

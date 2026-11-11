using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una app del celular (archivos de apps/ de un paquete), por ejemplo
    /// "app.instante". Su id es también la clave de su nombre en lang/. Las apps
    /// son parodias: nunca el nombre de una marca real.
    /// </summary>
    public sealed class AppDefinition
    {
        /// <summary>Id estable: <c>app.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Si viene instalada en todo celular nuevo (como el correo).</summary>
        [JsonProperty("de_fabrica")]
        public bool Preinstalled { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;
    }
}

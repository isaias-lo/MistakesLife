using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una forma de contratar un servicio, por ejemplo "servicio.celular.prepago".
    /// Su id empieza con el id del servicio y es también la clave de su nombre en lang/.
    /// </summary>
    public sealed class ServiceOptionDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Lo que cuesta al mes, en dinero del inicio del mundo.</summary>
        [JsonProperty("precio", Required = Required.Always)]
        public decimal Price { get; set; }
    }
}

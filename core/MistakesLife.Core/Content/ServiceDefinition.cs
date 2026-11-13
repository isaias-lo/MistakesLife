using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un servicio que el jugador contrata y paga cada mes, como el plan del
    /// celular (lista "economia.servicios" de country.json). Nada se cobra si el
    /// jugador no lo contrató: elige una de sus opciones en un evento
    /// (jugador.contratar('servicio.celular.prepago')) y la puede cambiar o cancelar.
    /// </summary>
    public sealed class ServiceDefinition
    {
        /// <summary>Id que empieza con "servicio.", por ejemplo "servicio.celular".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Cuándo se cobra lo contratado (por ejemplo, solo si tiene celular). Si falta, siempre.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>Si se divide en dos cuando tiene pareja (como el internet de la casa).</summary>
        [JsonProperty("compartido")]
        public bool Shared { get; set; }

        [JsonProperty("opciones", Required = Required.Always)]
        public List<ServiceOptionDefinition> Options { get; set; } = new List<ServiceOptionDefinition>();

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        /// <summary>La opción con ese id, o null si este servicio no la tiene.</summary>
        public ServiceOptionDefinition? Option(string id) => Options.FirstOrDefault(o => o.Id == id);
    }
}

using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una respuesta que el jugador puede mandar a un mensaje de Mensajito: su id es
    /// la clave del texto (<c>chat.resp.gracias</c>) y lo que cambia en quien escribió.
    /// </summary>
    public sealed class ChatReplyDefinition : ChatFeelingsDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Lo demás que pasa al contestar así, en el lenguaje de los eventos, con <c>persona</c>
        /// = quien escribió: ir al almuerzo del domingo, pedirle el dato de la pega. Vacío: solo cambia lo que siente.
        /// </summary>
        [JsonProperty("efectos")]
        public List<string> Scripts { get; set; } = new List<string>();

        [JsonIgnore]
        public List<Effect> CompiledScripts { get; } = new List<Effect>();
    }
}

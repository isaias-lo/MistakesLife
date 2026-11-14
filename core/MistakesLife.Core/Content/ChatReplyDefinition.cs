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
    }
}

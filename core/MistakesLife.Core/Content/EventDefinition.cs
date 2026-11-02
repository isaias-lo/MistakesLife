using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un evento tal como se escribe en un paquete JSON (carpeta events/):
    /// cuándo puede pasar, qué tan probable es, qué opciones ofrece y qué cambia cada una.
    /// </summary>
    public sealed class EventDefinition
    {
        /// <summary>Id estable, por ejemplo "evento.entrevista_testigo_copia".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Clave del texto que describe la escena. Por defecto, el mismo id.</summary>
        [JsonProperty("texto")]
        public string? TextKey { get; set; }

        /// <summary>Cuándo puede pasar (lenguaje de condiciones). Opcional.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>Qué tan probable es frente a otros eventos posibles.</summary>
        [JsonProperty("peso")]
        public int Weight { get; set; } = 10;

        /// <summary>Si es true, detiene el tiempo: el jugador lo vive con calma.</summary>
        [JsonProperty("dia_clave")]
        public bool KeyDay { get; set; }

        /// <summary>Si es true, solo puede pasar una vez en la vida del personaje.</summary>
        [JsonProperty("unico")]
        public bool Unique { get; set; }

        /// <summary>
        /// Si se indica, el evento no pasa al azar: es la escena que aparece
        /// cuando despierta una semilla de ese tipo.
        /// </summary>
        [JsonProperty("semilla")]
        public string? SeedType { get; set; }

        /// <summary>Efectos que se aplican apenas ocurre, antes de elegir.</summary>
        [JsonProperty("efectos")]
        public List<string> Effects { get; set; } = new List<string>();

        [JsonProperty("opciones")]
        public List<EventOptionDefinition> Options { get; set; } = new List<EventOptionDefinition>();

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResolvedTextKey => string.IsNullOrEmpty(TextKey) ? Id : TextKey!;

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public List<Effect> CompiledEffects { get; } = new List<Effect>();
    }
}

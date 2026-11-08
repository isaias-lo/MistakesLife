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
        /// Semanas que deben pasar antes de que el mismo evento pueda repetirse
        /// (0 = sin espera). Evita que un evento común aparezca seguido.
        /// </summary>
        [JsonProperty("enfriamiento_semanas")]
        public int CooldownWeeks { get; set; }

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

        /// <summary>
        /// Probabilidad de que ocurra en un año (0 a 1) mientras se cumplen sus condiciones,
        /// tomada de estadísticas reales (por ejemplo 0.04 para un robo). Si se usa, el
        /// evento no compite por peso con los demás: cada semana tiene su propia oportunidad.
        /// Opcional.
        /// </summary>
        [JsonProperty("probabilidad_anual")]
        public double? AnnualProbability { get; set; }

        /// <summary>Oportunidad semanal equivalente a la probabilidad anual.</summary>
        [JsonIgnore]
        public double WeeklyProbability =>
            AnnualProbability.HasValue ? 1.0 - System.Math.Pow(1.0 - AnnualProbability.Value, 1.0 / Time.GameDate.WeeksPerYear) : 0.0;

        /// <summary>
        /// Si es verdadero, al armar el contenido se agrega una opción por cada nivel
        /// de cada carrera cargada ("estudiar esto"), antes de las opciones propias.
        /// Así un país no necesita conocer las carreras que existen.
        /// </summary>
        [JsonProperty("opciones_carreras")]
        public bool CareerOptions { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>
        /// Si el evento viene del paquete de un país, ese país: solo le pasa a quien
        /// vive ahí. Null en los eventos del paquete base y de las carreras, que le
        /// pueden pasar a cualquiera.
        /// </summary>
        [JsonIgnore]
        public string? CountryId { get; internal set; }

        [JsonIgnore]
        public string ResolvedTextKey => string.IsNullOrEmpty(TextKey) ? Id : TextKey!;

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public List<Effect> CompiledEffects { get; } = new List<Effect>();
    }
}

using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una tarea real de la pega de una carrera (D5b): el cierre de mes, declarar el IVA, un bug
    /// en producción. Cada mes le toca una al jugador; es un momento de los
    /// eventos de la carrera (<c>jugador.tarea_pendiente == 'tarea.software.bug_produccion'</c>)
    /// donde elige cómo hacerla (<c>jugador.hacer_tarea('calma')</c>). Su id es también la clave
    /// de su nombre en la agenda ("Bug en producción").
    /// </summary>
    public sealed class CareerTaskDefinition
    {
        /// <summary>Por ejemplo "tarea.software.bug_produccion".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Cuándo le puede tocar (por ejemplo "jugador.area == 'carrera.contabilidad.area.impuestos'").
        /// Opcional: si falta, a cualquiera con un cargo en la carrera.
        /// </summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>
        /// El día del mes en que vence, si tiene una fecha real (el IVA, el 20). Opcional: sin día,
        /// las tareas del mes se reparten en sus semanas.
        /// </summary>
        [JsonProperty("dia")]
        public int? Day { get; set; }

        [JsonIgnore]
        public string CareerId { get; internal set; } = string.Empty;

        [JsonIgnore]
        internal Expression? CompiledConditions { get; set; }
    }
}

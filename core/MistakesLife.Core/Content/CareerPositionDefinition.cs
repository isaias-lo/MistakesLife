using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un cargo de la escalera de una carrera (junior, senior, gerente...). Para llegar a
    /// él hace falta tiempo en el cargo anterior, buen desempeño y sus requisitos.
    /// Su id es también la clave de su nombre.
    /// </summary>
    public sealed class CareerPositionDefinition
    {
        /// <summary>Por ejemplo "carrera.software.cargo.senior".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Cuánto paga este cargo frente al sueldo del título (2 = el doble). Si falta, 1.</summary>
        [JsonProperty("sueldo_relativo")]
        public double SalaryRelative { get; set; } = 1;

        /// <summary>Meses en el cargo anterior que hacen falta para ascender a este. Si falta, 0.</summary>
        [JsonProperty("meses_minimos")]
        public int MinMonths { get; set; }

        /// <summary>Desempeño mínimo (0 a 100) para ascender a este cargo. Si falta, 50.</summary>
        [JsonProperty("desempeno_minimo")]
        public int MinPerformance { get; set; } = 50;

        /// <summary>
        /// Otros requisitos para ascender a este cargo: formación, personalidad, contactos
        /// (por ejemplo "jugador.extraversion >= 35"). Opcional.
        /// </summary>
        [JsonProperty("requisitos")]
        public string? Requirements { get; set; }

        /// <summary>
        /// A qué cargos se puede ascender desde este. Si falta, al siguiente de la lista;
        /// una lista vacía marca la cima. Con dos o más, la carrera se divide en caminos.
        /// </summary>
        [JsonProperty("siguientes")]
        public List<string>? Next { get; set; }

        [JsonIgnore]
        public string CareerId { get; internal set; } = string.Empty;

        /// <summary>Los cargos a los que se asciende desde este (vacío en la cima).</summary>
        [JsonIgnore]
        public IReadOnlyList<CareerPositionDefinition> NextPositions { get; internal set; } = new List<CareerPositionDefinition>();

        /// <summary>El cargo desde el que se llega a este, o null si es el inicial.</summary>
        [JsonIgnore]
        public CareerPositionDefinition? Previous { get; internal set; }

        /// <summary>Cuántos ascensos hay desde el cargo inicial hasta este (el inicial es 0).</summary>
        [JsonIgnore]
        public int Depth { get; internal set; }

        [JsonIgnore]
        public bool IsTop => NextPositions.Count == 0;

        [JsonIgnore]
        internal Expression? CompiledRequirements { get; set; }
    }
}

using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un área de una carrera (por ejemplo ciberseguridad en software o auditoría en
    /// contabilidad): quién puede trabajar en ella, cuánto paga y su escalera de cargos.
    /// Su id es también la clave de su nombre.
    /// </summary>
    public sealed class CareerAreaDefinition
    {
        /// <summary>Por ejemplo "carrera.software.area.ciberseguridad".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Quién puede entrar (por ejemplo "jugador.tiene('certificacion_ciberseguridad')").
        /// Opcional: sin requisitos, cualquier titulado de la carrera puede.
        /// </summary>
        [JsonProperty("requisitos")]
        public string? Requirements { get; set; }

        /// <summary>Cuánto paga esta área frente al sueldo del título (1.2 = 20 % más). Si falta, 1.</summary>
        [JsonProperty("sueldo_relativo")]
        public double SalaryRelative { get; set; } = 1;

        /// <summary>
        /// Escalera propia de esta área. Opcional: si falta, usa los cargos de la carrera.
        /// </summary>
        [JsonProperty("cargos")]
        public List<CareerPositionDefinition>? Positions { get; set; }

        [JsonIgnore]
        public string CareerId { get; internal set; } = string.Empty;

        /// <summary>Los cargos de esta área, del inicial a los más altos (los propios o los de la carrera).</summary>
        [JsonIgnore]
        public IReadOnlyList<CareerPositionDefinition> Ladder { get; internal set; } = new List<CareerPositionDefinition>();

        /// <summary>El primer cargo: donde empieza quien llega sin experiencia.</summary>
        [JsonIgnore]
        public CareerPositionDefinition EntryPosition => Ladder[0];

        [JsonIgnore]
        internal Expression? CompiledRequirements { get; set; }
    }
}

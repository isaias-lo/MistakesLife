using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una carrera (archivo career.json de un paquete de carrera): su id, sus niveles
    /// de estudio, sus áreas y su escalera de cargos. Lo que pasa mientras se estudia o
    /// trabaja va en los eventos del paquete.
    /// </summary>
    public sealed class CareerDefinition
    {
        /// <summary>
        /// Tipos de lugar de trabajo que la parte gráfica sabe dibujar. Son genéricos:
        /// cualquier carrera elige uno (por ejemplo, una carrera de diseño también usaría
        /// la oficina abierta). Agregar uno nuevo obliga a dibujarlo en Unity.
        /// </summary>
        public static readonly string[] WorkplaceKinds =
        {
            "lugar_trabajo.oficina_abierta",
            "lugar_trabajo.oficina_cubiculos",
            "lugar_trabajo.consulta",
        };

        /// <summary>Por ejemplo "carrera.contabilidad". Es también la clave de su nombre.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("niveles", Required = Required.Always)]
        public List<CareerLevelDefinition> Levels { get; set; } = new List<CareerLevelDefinition>();

        /// <summary>
        /// Las áreas donde se puede trabajar (mínimo tres, como pide el diseño). Opcional
        /// para carreras de prueba: sin áreas, el trabajo se crea con trabajar(sueldo).
        /// </summary>
        [JsonProperty("areas")]
        public List<CareerAreaDefinition> Areas { get; set; } = new List<CareerAreaDefinition>();

        /// <summary>La escalera de cargos de la carrera, del inicial a la cima. Un área puede tener la suya.</summary>
        [JsonProperty("cargos")]
        public List<CareerPositionDefinition> Positions { get; set; } = new List<CareerPositionDefinition>();

        /// <summary>
        /// Cómo se ve el lugar donde trabaja quien tiene este título (uno de
        /// <see cref="WorkplaceKinds"/>). Opcional: sin él se dibuja una oficina común.
        /// </summary>
        [JsonProperty("lugar_de_trabajo")]
        public string? WorkplaceKind { get; set; }

        [JsonIgnore]
        public string PackId { get; internal set; } = string.Empty;
    }
}

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
            "lugar_trabajo.tienda",
            "lugar_trabajo.restoran",
            "lugar_trabajo.obra",
        };

        /// <summary>Por ejemplo "carrera.contabilidad". Es también la clave de su nombre.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Los niveles de estudio. Un oficio no tiene (se aprende trabajando).</summary>
        [JsonProperty("niveles")]
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
        /// Los ramos de la carrera, por semestre. Cada nivel toma los de su institución
        /// y largo; cada semestre de cada nivel tiene que tener al menos uno.
        /// </summary>
        [JsonProperty("ramos")]
        public List<CourseDefinition> Courses { get; set; } = new List<CourseDefinition>();

        /// <summary>
        /// Cómo se ve el lugar donde trabaja quien tiene este título (uno de
        /// <see cref="WorkplaceKinds"/>). Opcional: sin él se dibuja una oficina común.
        /// </summary>
        /// <summary>
        /// Las tareas reales de la pega (D5b): cada mes le toca una al jugador que tiene un
        /// cargo en esta carrera. Opcional: sin tareas, el desempeño solo lo mueven los eventos.
        /// </summary>
        [JsonProperty("tareas")]
        public List<CareerTaskDefinition> Tasks { get; set; } = new List<CareerTaskDefinition>();

        [JsonProperty("lugar_de_trabajo")]
        public string? WorkplaceKind { get; set; }

        /// <summary>
        /// Nombres para las empresas de esta carrera (claves de texto que empiezan con
        /// "empresa."). Cada empresa nueva toma uno que no se esté usando.
        /// </summary>
        [JsonProperty("nombres_empresa")]
        public List<string> CompanyNames { get; set; } = new List<string>();

        /// <summary>
        /// Marca que recibe quien entra a trabajar en esta carrera al aceptar una
        /// oferta (por ejemplo "trabaja_software"), para los eventos de la carrera.
        /// Opcional.
        /// </summary>
        [JsonProperty("marca_trabajo")]
        public string? WorkFlag { get; set; }

        /// <summary>
        /// Si es un oficio (se trabaja sin título; su id empieza con "oficio."): sueldo, meta y
        /// pago variable. Null en una carrera de estudios.
        /// </summary>
        [JsonProperty("oficio")]
        public OccupationDefinition? Occupation { get; set; }

        [JsonIgnore]
        public bool IsOccupation => Occupation != null;

        [JsonIgnore]
        public string PackId { get; internal set; } = string.Empty;
    }
}

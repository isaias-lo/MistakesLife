using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Cómo es un curso del colegio en un país (campo "curso" de country.json).
    /// </summary>
    public sealed class SchoolClassDefinition
    {
        /// <summary>Cuántos alumnos tiene un curso, contándote ("alumnos").</summary>
        [JsonProperty("alumnos", Required = Required.Always)]
        public int Students { get; set; }

        /// <summary>
        /// Con cuántos compañeros de ese curso el jugador tiene trato: tienen nombre,
        /// sienten cosas por él y aparecen en los momentos ("con_nombre"). Los demás
        /// están en la sala, pero no se juegan.
        /// </summary>
        [JsonProperty("con_nombre", Required = Required.Always)]
        public int Named { get; set; }

        /// <summary>
        /// Probabilidad (0 a 1) de que la profe o el profe jefe cambie de un año al otro
        /// dentro de la misma etapa ("cambio_profe_jefe").
        /// </summary>
        [JsonProperty("cambio_profe_jefe")]
        public double TeacherChange { get; set; } = 0.3;

        /// <summary>
        /// Probabilidad (0 a 1) de que, al llegar a una etapa nueva del colegio (de básica
        /// a media), un compañero siga en tu curso ("siguen_al_cambiar_etapa").
        /// </summary>
        [JsonProperty("siguen_al_cambiar_etapa")]
        public double KeepOnNewStage { get; set; } = 0.4;

        /// <summary>
        /// Probabilidad (0 a 1) de que un año alguien se cambie de colegio y otro llegue
        /// nuevo ("cambios_por_ano").
        /// </summary>
        [JsonProperty("cambios_por_ano")]
        public double YearlyChange { get; set; } = 0.3;

        /// <summary>Probabilidad (0 a 1) de que quien hace clases en esta etapa sea mujer ("profes_mujeres").</summary>
        [JsonProperty("profes_mujeres")]
        public double FemaleTeachers { get; set; } = 0.7;
    }
}

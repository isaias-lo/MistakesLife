using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una forma de buscar trabajo en un país, por ejemplo "busqueda.forma.portales"
    /// (mandar currículums por internet) o "busqueda.forma.contactos" (pedirle a un
    /// conocido que te recomiende). La elige el jugador al empezar a buscar.
    /// </summary>
    public sealed class JobSearchMethodDefinition
    {
        /// <summary>Empieza con "busqueda.forma.". También es la clave de su texto.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Cuántas postulaciones manda por semana (para contarlas y mostrarlas).</summary>
        [JsonProperty("postulaciones_semana")]
        public int ApplicationsPerWeek { get; set; } = 1;

        /// <summary>
        /// Probabilidad por semana de que lo llamen a una entrevista (0 a 1), antes de
        /// su experiencia, su edad, el tiempo que lleva buscando y el ciclo económico.
        /// </summary>
        [JsonProperty("entrevista_semanal", Required = Required.Always)]
        public double WeeklyInterview { get; set; }

        /// <summary>
        /// Cuánto suma (o resta) a la probabilidad de quedar después de la entrevista:
        /// buscar con cuidado o con una recomendación ayuda; mandar currículums a todo, no.
        /// </summary>
        [JsonProperty("exito_extra")]
        public double ExtraSuccess { get; set; }

        /// <summary>
        /// Si solo sirve teniendo un conocido que trabaje y lo aprecie (amistad o
        /// colega con cariño suficiente): esa persona queda como su contacto.
        /// </summary>
        [JsonProperty("necesita_contacto")]
        public bool NeedsContact { get; set; }
    }
}

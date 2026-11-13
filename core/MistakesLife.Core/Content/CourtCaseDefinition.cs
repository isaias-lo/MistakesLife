using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tipo de causa judicial tal como se escribe en un paquete de país (carpeta
    /// courts/): el cobro de una deuda, un divorcio... Dice en qué tribunal se ve y
    /// por qué etapas pasa, cada una con su plazo. Una causa nunca empieza sola: la
    /// abre un evento con <c>abrir_causa('causa.x')</c>, por algo que el jugador hizo
    /// antes (dejó de pagar, se casó y se separó). Lo que pasa en cada etapa lo
    /// deciden los eventos que leen <c>causa_en('causa.x', 'etapa')</c>.
    /// </summary>
    public sealed class CourtCaseDefinition
    {
        /// <summary>Id estable, por ejemplo "causa.chile.cobro_deuda". Es también la clave de su nombre en lang/.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Clave del nombre del tribunal, por ejemplo "tribunal.chile.civil".</summary>
        [JsonProperty("tribunal", Required = Required.Always)]
        public string CourtKey { get; set; } = string.Empty;

        /// <summary>Letra con que empieza el RIT o rol de la causa, por ejemplo "C" (civil).</summary>
        [JsonProperty("prefijo_rit", Required = Required.Always)]
        public string RitPrefix { get; set; } = string.Empty;

        /// <summary>Las etapas en orden. La causa empieza en la primera.</summary>
        [JsonProperty("etapas", Required = Required.Always)]
        public List<CourtStageDefinition> Stages { get; set; } = new List<CourtStageDefinition>();

        /// <summary>
        /// Cómo puede terminar ("pago", "acogida", "rechazada"...). Un evento la cierra con
        /// <c>causa('causa.x').cerrar('pago')</c>; si la última etapa no espera, termina
        /// sola como "terminada" (que entonces debe estar en la lista). El texto de cada
        /// resultado es el id de la causa + ".resultado." + el resultado.
        /// </summary>
        [JsonProperty("resultados", Required = Required.Always)]
        public List<string> Outcomes { get; set; } = new List<string>();

        /// <summary>
        /// Si la causa se termina cuando muere la otra parte (un divorcio: no hay de quién
        /// divorciarse). Termina como "fallecimiento", que entonces debe estar en resultados.
        /// </summary>
        [JsonProperty("termina_si_muere_la_otra_parte")]
        public bool EndsIfOtherPartyDies { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>El país del paquete donde viene la causa (las causas son siempre de un país).</summary>
        [JsonIgnore]
        public string? CountryId { get; internal set; }

        /// <summary>Clave del texto de un resultado.</summary>
        public string OutcomeTextKey(string outcome) => Id + ".resultado." + outcome;

        /// <summary>La etapa con ese id, o null.</summary>
        public CourtStageDefinition? Stage(string id) => Stages.Find(s => s.Id == id);

        /// <summary>Posición de la etapa con ese id, o -1.</summary>
        public int StageIndex(string id) => Stages.FindIndex(s => s.Id == id);
    }
}

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.busqueda_trabajo" de country.json: cómo se busca trabajo en el
    /// país. El jugador elige una forma de buscar; cada semana puede llegar una
    /// entrevista y, si le va bien, una oferta que él acepta, negocia o rechaza.
    /// </summary>
    public sealed class JobSearchDefinition
    {
        /// <summary>Las formas de buscar trabajo del país (al menos una).</summary>
        [JsonProperty("formas", Required = Required.Always)]
        public List<JobSearchMethodDefinition> Methods { get; set; } = new List<JobSearchMethodDefinition>();

        /// <summary>Probabilidad base de quedar después de una entrevista (0 a 1).</summary>
        [JsonProperty("exito_entrevista")]
        public double InterviewSuccess { get; set; } = 0.4;

        /// <summary>Cuánto más lo llaman por cada año cotizado de experiencia (0.05 = 5 % más por año).</summary>
        [JsonProperty("factor_por_ano_experiencia")]
        public double PerYearOfExperience { get; set; }

        /// <summary>Tope de lo que suma la experiencia (1.5 = como mucho 50 % más).</summary>
        [JsonProperty("tope_experiencia")]
        public double ExperienceCap { get; set; } = 1;

        /// <summary>Cuánto más lo llaman si tiene un título (1.2 = 20 % más).</summary>
        [JsonProperty("factor_titulo")]
        public double DegreeFactor { get; set; } = 1;

        /// <summary>Desde qué edad cuesta más que lo llamen (discriminación por edad). Si falta, nunca.</summary>
        [JsonProperty("edad_dificil")]
        public int? HardAge { get; set; }

        /// <summary>Cuánto lo llaman desde esa edad (0.6 = 40 % menos).</summary>
        [JsonProperty("factor_edad")]
        public double AgeFactor { get; set; } = 1;

        /// <summary>Semanas buscando desde las que se nota la cesantía larga en el currículum. Si falta, nunca.</summary>
        [JsonProperty("semanas_cesantia_larga")]
        public int? LongSearchWeeks { get; set; }

        /// <summary>Cuánto lo llaman con una cesantía larga (0.7 = 30 % menos).</summary>
        [JsonProperty("factor_cesantia_larga")]
        public double LongSearchFactor { get; set; } = 1;

        /// <summary>
        /// Meses cotizados desde los que alguien sin título deja de ganar el sueldo
        /// mínimo en un trabajo nuevo y gana lo típico sin título.
        /// </summary>
        [JsonProperty("meses_para_sueldo_tipico")]
        public int MonthsForTypicalSalary { get; set; } = 12;

        /// <summary>Cariño mínimo que debe tenerle un conocido para recomendarlo.</summary>
        [JsonProperty("carino_contacto")]
        public int ContactAffection { get; set; } = 50;

        /// <summary>Qué pasa al pedir más sueldo.</summary>
        [JsonProperty("negociacion")]
        public JobNegotiationDefinition Negotiation { get; set; } = new JobNegotiationDefinition();

        public JobSearchMethodDefinition? Method(string id) => Methods.FirstOrDefault(m => m.Id == id);
    }
}

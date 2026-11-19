using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "trabajo_de_la_gente" de country.json: en qué anda la gente que nadie juega
    /// (tu familia, tus amistades, tus conocidos), con los datos reales del país. Cada año,
    /// en su cumpleaños, se ve si trabaja, busca pega, estudia o está jubilada, según su
    /// edad. Quien trabaja tiene una de las pegas que existen enteras en el juego solo en
    /// la parte real de los ocupados que trabaja en ella; el resto "trabaja" sin que el
    /// juego le invente una pega.
    /// </summary>
    public sealed class PeopleWorkDefinition
    {
        /// <summary>Desde qué edad se mira (en Chile, 15: así mide la encuesta de empleo).</summary>
        [JsonProperty("edad_min")]
        public int MinAge { get; set; } = 15;

        /// <summary>Tramos de edad con cuántos trabajan y cuánto desempleo hay.</summary>
        [JsonProperty("edades")]
        public List<PeopleWorkAgeDefinition> Ages { get; set; } = new List<PeopleWorkAgeDefinition>();

        /// <summary>Quiénes estudian una carrera (opcional).</summary>
        [JsonProperty("estudia_superior")]
        public PeopleStudyDefinition? HigherEducation { get; set; }

        /// <summary>
        /// La parte de los ocupados que son mujeres (de 0 a 1). Sirve para repartir una pega
        /// por sexo cuando la pega dice su propia parte de mujeres.
        /// </summary>
        [JsonProperty("mujeres_ocupadas")]
        public double WomenShare { get; set; } = 0.5;

        /// <summary>Las pegas que existen enteras en el juego y la parte real de los ocupados que trabaja en cada una.</summary>
        [JsonProperty("pegas")]
        public List<PeopleJobDefinition> Jobs { get; set; } = new List<PeopleJobDefinition>();

        /// <summary>El tramo de esa edad, o null si no hay datos para ella.</summary>
        public PeopleWorkAgeDefinition? AgeGroup(int age) => Ages.FirstOrDefault(a => age >= a.From && age <= a.To);
    }
}

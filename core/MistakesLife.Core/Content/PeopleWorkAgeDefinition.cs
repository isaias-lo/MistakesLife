using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>Un tramo de edad de "trabajo_de_la_gente": cuántos trabajan y cuánto desempleo hay.</summary>
    public sealed class PeopleWorkAgeDefinition
    {
        /// <summary>Primera edad del tramo.</summary>
        [JsonProperty("desde", Required = Required.Always)]
        public int From { get; set; }

        /// <summary>Última edad del tramo (incluida).</summary>
        [JsonProperty("hasta", Required = Required.Always)]
        public int To { get; set; }

        /// <summary>Tasa de ocupación: la parte de la gente de esa edad que trabaja (de 0 a 1).</summary>
        [JsonProperty("ocupados", Required = Required.Always)]
        public double Employed { get; set; }

        /// <summary>Tasa de desocupación: de los que trabajan o buscan, la parte que busca (de 0 a 1).</summary>
        [JsonProperty("desocupacion", Required = Required.Always)]
        public double Unemployment { get; set; }

        /// <summary>
        /// La parte de la gente de esa edad que busca pega: los ocupados por la tasa de
        /// desocupación sobre los que no están desocupados.
        /// </summary>
        public double Unemployed => Unemployment >= 1 ? 0 : Employed * Unemployment / (1 - Unemployment);
    }
}

using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tramo de vacaciones legales por antigüedad (ver <see cref="VacationsDefinition"/>):
    /// desde tantos años con el mismo empleador, tantos días al año.
    /// </summary>
    public sealed class VacationTierDefinition
    {
        /// <summary>Desde cuántos años con el mismo empleador vale este tramo (0 = desde el primer día).</summary>
        [JsonProperty("desde_anos")]
        public int FromYears { get; set; }

        /// <summary>Días de vacaciones al año en este tramo (en la unidad del país: hábiles o corridos).</summary>
        [JsonProperty("dias", Required = Required.Always)]
        public int Days { get; set; }
    }
}

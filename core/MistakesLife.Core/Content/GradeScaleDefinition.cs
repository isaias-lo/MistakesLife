using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Cómo se ponen las notas en un país ("escala_notas" de country.json). En Chile, de 1,0
    /// a 7,0 con un decimal, y se aprueba con 4,0.
    /// </summary>
    public sealed class GradeScaleDefinition
    {
        [JsonProperty("minima", Required = Required.Always)]
        public double Min { get; set; }

        [JsonProperty("maxima", Required = Required.Always)]
        public double Max { get; set; }

        /// <summary>La nota con que se aprueba (incluida).</summary>
        [JsonProperty("aprueba", Required = Required.Always)]
        public double Pass { get; set; }

        /// <summary>Con cuántos decimales se pone una nota (1 en Chile; 0 si son enteras).</summary>
        [JsonProperty("decimales")]
        public int Decimals { get; set; } = 1;

        /// <summary>Cuántas notas tiene un ramo en un año (sumando todas sus pruebas y trabajos).</summary>
        [JsonProperty("notas_por_ano", Required = Required.Always)]
        public int MarksPerYear { get; set; }
    }
}

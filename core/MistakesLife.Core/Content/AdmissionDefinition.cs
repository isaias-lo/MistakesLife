using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Cómo se entra a la universidad en un país (D5a5). En Chile: la PAES del DEMRE, con el
    /// NEM (las notas de 1º a 4º medio), el ranking (cuánto destacaste en tu colegio) y las
    /// pruebas obligatorias (Competencia Lectora y Matemática), todo de 100 a 1.000 puntos.
    /// Cada carrera pondera esos puntajes a su manera y tiene su corte (ver
    /// <see cref="LevelAdmissionDefinition"/>). Opcional: sin ella, nadie da prueba.
    /// </summary>
    public sealed class AdmissionDefinition
    {
        /// <summary>El puntaje más bajo de la escala (100 en la PAES).</summary>
        public const int MinScore = 100;

        /// <summary>El puntaje más alto de la escala (1.000 en la PAES).</summary>
        public const int MaxScore = 1000;

        /// <summary>
        /// El promedio de las pruebas obligatorias que pide la universidad para poder postular
        /// (458 en Chile desde la Admisión 2024). Debajo de eso, solo institutos, CFT y
        /// universidades fuera del sistema.
        /// </summary>
        [JsonProperty("minimo_postulacion")]
        public int MinimumToApply { get; set; }

        /// <summary>
        /// El promedio de notas de un colegio típico (de 1 a 7). Desde ahí el ranking sube
        /// sobre el NEM: quien tiene mejores notas que su colegio suma puntos.
        /// </summary>
        [JsonProperty("promedio_colegio")]
        public double SchoolAverage { get; set; }

        /// <summary>Las pruebas obligatorias, cada una con el ramo del colegio que mide.</summary>
        [JsonProperty("pruebas")]
        public List<AdmissionTestDefinition> Tests { get; set; } = new List<AdmissionTestDefinition>();
    }
}

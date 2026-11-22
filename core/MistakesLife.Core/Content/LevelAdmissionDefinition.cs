using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Qué pide un nivel de carrera para entrar (D5a5): cuánto pesa cada puntaje ("nem",
    /// "ranking" y cada prueba del país, que suman 1) y el puntaje ponderado del último que
    /// quedó (el corte). Si un nivel no la tiene, no hay selección por puntaje.
    /// </summary>
    public sealed class LevelAdmissionDefinition
    {
        /// <summary>Nombre de la ponderación del NEM.</summary>
        public const string Nem = "nem";

        /// <summary>Nombre de la ponderación del ranking.</summary>
        public const string Ranking = "ranking";

        /// <summary>El puntaje ponderado del último que quedó, de 100 a 1.000.</summary>
        [JsonProperty("corte")]
        public double Cutoff { get; set; }

        /// <summary>Cuánto pesa cada puntaje (de 0 a 1; todos suman 1).</summary>
        [JsonProperty("ponderacion")]
        public SortedDictionary<string, double> Weights { get; set; } = new SortedDictionary<string, double>(System.StringComparer.Ordinal);
    }
}

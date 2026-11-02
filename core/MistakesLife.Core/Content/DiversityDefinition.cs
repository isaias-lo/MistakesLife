using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "diversidad" de country.json: en qué proporción nacen las
    /// orientaciones e identidades de género en ese país, según encuestas.
    /// Son pesos (por ejemplo, por cada mil personas).
    /// </summary>
    public sealed class DiversityDefinition
    {
        /// <summary>Claves: heterosexual, homosexual, bisexual, asexual.</summary>
        [JsonProperty("orientaciones")]
        public Dictionary<string, int> Orientations { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Claves: cis (siente el género de su sexo), trans (siente el otro) y
        /// no_binario.
        /// </summary>
        [JsonProperty("identidades")]
        public Dictionary<string, int> Identities { get; set; } = new Dictionary<string, int>();

        public static readonly IReadOnlyList<string> OrientationKeys = new[] { "heterosexual", "homosexual", "bisexual", "asexual" };

        public static readonly IReadOnlyList<string> IdentityKeys = new[] { "cis", "trans", "no_binario" };
    }
}

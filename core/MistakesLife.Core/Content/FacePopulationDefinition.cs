using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "rostros" de country.json: cuántas personas del país tienen cada tono de piel,
    /// color de ojos y color de pelo (por cada cien), y cuántas son miopes (por cada mil).
    /// Solo se usa para quienes no tienen papás en el mundo: el resto hereda.
    /// Ver <see cref="Life.FaceRules"/>.
    /// </summary>
    public sealed class FacePopulationDefinition
    {
        /// <summary>Seis tonos, de muy claro a muy oscuro (los de la pieza de piel).</summary>
        [JsonProperty("piel")]
        public List<int> Skin { get; set; } = new List<int>();

        /// <summary>Café oscuro, café claro, verdes y azules.</summary>
        [JsonProperty("ojos")]
        public List<int> Eyes { get; set; } = new List<int>();

        /// <summary>Negro, castaño oscuro, castaño, rubio y colorín.</summary>
        [JsonProperty("pelo")]
        public List<int> Hair { get; set; } = new List<int>();

        /// <summary>Cuántos adultos de cada mil son miopes (usan lentes desde chicos o jóvenes).</summary>
        [JsonProperty("miopes_por_mil")]
        public int MyopiaPerThousand { get; set; } = 250;
    }
}

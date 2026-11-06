using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.empresas" de country.json: de qué tamaño son las empresas
    /// del país, cuánta gente trabaja en cada tamaño y cómo viven (crecen, se achican
    /// o quiebran). Si falta, los trabajos no tienen empresa.
    /// </summary>
    public sealed class CompaniesDefinition
    {
        /// <summary>Los tamaños, del más chico al más grande.</summary>
        [JsonProperty("tamanos", Required = Required.Always)]
        public List<CompanySizeDefinition> Sizes { get; set; } = new List<CompanySizeDefinition>();

        /// <summary>
        /// Probabilidad de que alguien que entra a trabajar llegue a una empresa
        /// donde ya trabaja otro personaje (del mismo tamaño y carrera), en vez de
        /// a una nueva. De 0 a 1.
        /// </summary>
        [JsonProperty("misma_empresa")]
        public double SameCompany { get; set; } = 0.5;

        public CompanySizeDefinition? Size(string id) => Sizes.FirstOrDefault(s => s.Id == id);

        /// <summary>El tamaño que sigue (más grande), o null si ya es el más grande.</summary>
        public CompanySizeDefinition? Bigger(string id)
        {
            int index = Sizes.FindIndex(s => s.Id == id);
            return index >= 0 && index + 1 < Sizes.Count ? Sizes[index + 1] : null;
        }

        /// <summary>El tamaño anterior (más chico), o null si ya es el más chico.</summary>
        public CompanySizeDefinition? Smaller(string id)
        {
            int index = Sizes.FindIndex(s => s.Id == id);
            return index > 0 ? Sizes[index - 1] : null;
        }
    }
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Los datos de un país, tal como se escriben en el archivo country.json
    /// de un paquete de país.
    /// </summary>
    public sealed class CountryDefinition
    {
        /// <summary>Por ejemplo "pais.chile".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Código de moneda, por ejemplo "CLP".</summary>
        [JsonProperty("moneda")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>Qué tan probable es nacer en este país frente a otros.</summary>
        [JsonProperty("peso_nacimiento")]
        public int BirthWeight { get; set; } = 1;

        /// <summary>Edad mínima y máxima de la madre al tener un hijo.</summary>
        [JsonProperty("edad_madre_min")]
        public int MotherAgeMin { get; set; } = 18;

        [JsonProperty("edad_madre_max")]
        public int MotherAgeMax { get; set; } = 42;

        /// <summary>
        /// Si es true, el hijo lleva el primer apellido del padre y luego el de la madre
        /// (como en Chile y Argentina en la práctica).
        /// </summary>
        [JsonProperty("dos_apellidos")]
        public bool TwoSurnames { get; set; } = true;

        [JsonProperty("nombres")]
        public NamePool Names { get; set; } = new NamePool();

        [JsonProperty("ciudades")]
        public List<CityDefinition> Cities { get; set; } = new List<CityDefinition>();

        [JsonProperty("etapas_educacion")]
        public List<EducationStageDefinition> EducationStages { get; set; } = new List<EducationStageDefinition>();

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>La etapa que corresponde a una edad, o null.</summary>
        public EducationStageDefinition? StageForAge(int ageInYears)
        {
            foreach (EducationStageDefinition stage in EducationStages)
            {
                if (ageInYears >= stage.FromAge && ageInYears < stage.ToAge)
                {
                    return stage;
                }
            }

            return null;
        }
    }
}

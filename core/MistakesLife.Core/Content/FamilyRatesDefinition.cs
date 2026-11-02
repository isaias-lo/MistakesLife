using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "familia_no_jugada" de country.json: qué tan seguido los familiares
    /// que nadie juega (hijos y nietos del jugador) forman pareja, tienen hijos o
    /// se separan. El jugador vive eso con eventos; ellos, con estas probabilidades.
    /// Todas son probabilidades por año, de 0 a 1.
    /// </summary>
    public sealed class FamilyRatesDefinition
    {
        /// <summary>Probabilidad al año de formar pareja, para un adulto sin pareja.</summary>
        [JsonProperty("pareja_anual")]
        public double PartnerPerYear { get; set; }

        /// <summary>Probabilidad al año de tener un hijo, para una pareja en edad fértil.</summary>
        [JsonProperty("hijo_anual")]
        public double ChildPerYear { get; set; }

        /// <summary>Probabilidad al año de que una pareja se separe.</summary>
        [JsonProperty("separacion_anual")]
        public double SeparationPerYear { get; set; }

        /// <summary>
        /// Probabilidad al año de adoptar, para una pareja del mismo sexo (que no
        /// puede tener hijos biológicos juntos). Pon 0 si la ley del país no lo permite.
        /// </summary>
        [JsonProperty("adopcion_anual")]
        public double AdoptionPerYear { get; set; }

        /// <summary>Edad desde la que forman pareja.</summary>
        [JsonProperty("edad_pareja_min")]
        public int PartnerMinAge { get; set; } = 18;

        /// <summary>Edad máxima de la mujer de la pareja para tener hijos.</summary>
        [JsonProperty("edad_fertil_max")]
        public int FertileMaxAge { get; set; } = 44;

        /// <summary>Máximo de hijos vivos con que alguien sigue buscando otro.</summary>
        [JsonProperty("max_hijos")]
        public int MaxChildren { get; set; } = 3;
    }
}

using System;

namespace MistakesLife.Core.Characters
{
    /// <summary>Lugar donde vive un personaje, con ids estables de los paquetes.</summary>
    public sealed class Residence
    {
        public Residence(string countryId, string cityId, string communeId)
        {
            CountryId = countryId ?? throw new ArgumentNullException(nameof(countryId));
            CityId = cityId ?? throw new ArgumentNullException(nameof(cityId));
            CommuneId = communeId ?? throw new ArgumentNullException(nameof(communeId));
        }

        /// <summary>Por ejemplo "pais.chile".</summary>
        public string CountryId { get; }

        /// <summary>Por ejemplo "ciudad.santiago".</summary>
        public string CityId { get; }

        /// <summary>Por ejemplo "comuna.puente_alto".</summary>
        public string CommuneId { get; }
    }
}

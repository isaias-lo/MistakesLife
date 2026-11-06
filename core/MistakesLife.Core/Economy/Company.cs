using System;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Una empresa del mundo, de una carrera (por ejemplo, una empresa de software).
    /// Su tamaño dice cuánto paga y qué cargos existen en ella; con los años crece, se
    /// achica o quiebra (ver <see cref="CompanySystem"/>). Nace cuando alguien entra a
    /// trabajar y no hay una empresa conocida que le sirva.
    /// </summary>
    public sealed class Company
    {
        public Company(int id, string countryId, string careerId, string sizeId, string? nameId, GameDate founded)
        {
            Id = id;
            CountryId = countryId ?? throw new ArgumentNullException(nameof(countryId));
            CareerId = careerId ?? throw new ArgumentNullException(nameof(careerId));
            SizeId = sizeId ?? throw new ArgumentNullException(nameof(sizeId));
            NameId = nameId;
            Founded = founded;
        }

        public int Id { get; }

        public string CountryId { get; }

        public string CareerId { get; }

        /// <summary>Tamaño actual, por ejemplo "empresa.tamano.mediana" (también es clave de texto).</summary>
        public string SizeId { get; internal set; }

        /// <summary>Clave de texto de su nombre, o null si la carrera no trae nombres.</summary>
        public string? NameId { get; }

        /// <summary>Desde cuándo la conoce el mundo (cuando entró la primera persona que se juega).</summary>
        public GameDate Founded { get; }

        /// <summary>Cuándo quebró, o null si sigue abierta.</summary>
        public GameDate? ClosedDate { get; internal set; }

        public bool IsOpen => !ClosedDate.HasValue;
    }
}

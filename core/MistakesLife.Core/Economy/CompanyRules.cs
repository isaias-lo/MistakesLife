using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Las reglas de las empresas, iguales para todo país: a qué empresa llega quien
    /// entra a trabajar, cuánto paga y qué cargos existen según su tamaño. Los tamaños
    /// y sus números los define cada país (economia.empresas).
    /// </summary>
    public static class CompanyRules
    {
        /// <summary>Las empresas del país donde vive, o null si el país no las define.</summary>
        public static CompaniesDefinition? Definition(ContentDatabase content, Character character) =>
            character.Residence == null ? null : content.GetCountry(character.Residence.CountryId).Economy?.Companies;

        /// <summary>El tamaño de la empresa donde trabaja, o null si no trabaja en una empresa.</summary>
        public static CompanySizeDefinition? SizeOf(World world, ContentDatabase content, Character character)
        {
            Company? company = world.CompanyById(character.Job?.CompanyId);
            return company == null ? null : content.GetCountry(company.CountryId).Economy?.Companies?.Size(company.SizeId);
        }

        /// <summary>
        /// Busca empresa para quien entra a trabajar en su carrera: de un tamaño al azar
        /// según cuánta gente trabaja en cada tamaño en su país (o del tamaño pedido), a
        /// veces la misma donde ya trabaja otro personaje y si no una nueva. Null si su
        /// país no define empresas.
        /// </summary>
        public static Company? Hire(World world, ContentDatabase content, Character character, CareerDefinition career, string? sizeId = null, int? excludeCompanyId = null)
        {
            CompaniesDefinition? companies = Definition(content, character);
            if (companies == null || companies.Sizes.Count == 0)
            {
                return null;
            }

            string countryId = character.Residence!.CountryId;
            GameRandom random = world.Random.Fork("empresa.contratar." + character.Id + "." + world.Date.TotalWeeks + "." + world.Companies.Count);
            CompanySizeDefinition size = (sizeId == null ? null : companies.Size(sizeId))
                ?? companies.Sizes[random.PickWeightedIndex(companies.Sizes.Select(s => (int)System.Math.Round(s.Weight * 1000)).ToList())];

            var withPeople = new HashSet<int>(world.Characters
                .Where(c => c.IsAlive && c.Id != character.Id && c.Job?.CompanyId != null)
                .Select(c => c.Job!.CompanyId!.Value));
            List<Company> known = world.Companies
                .Where(c => c.IsOpen && c.CountryId == countryId && c.CareerId == career.Id && c.SizeId == size.Id
                    && c.Id != excludeCompanyId && withPeople.Contains(c.Id))
                .ToList();
            if (known.Count > 0 && random.Chance(companies.SameCompany))
            {
                return random.Pick(known);
            }

            var used = new HashSet<string>(world.Companies.Where(c => c.IsOpen && c.NameId != null).Select(c => c.NameId!));
            List<string> free = career.CompanyNames.Where(n => !used.Contains(n)).ToList();
            string? name = free.Count > 0 ? random.Pick(free) : career.CompanyNames.Count > 0 ? random.Pick(career.CompanyNames) : null;
            return world.CreateCompany(countryId, career.Id, size.Id, name);
        }

        /// <summary>El cargo más alto que existe en su empresa (contado desde el inicial), o null si no hay tope.</summary>
        public static int? MaxDepth(World world, ContentDatabase content, Character character) =>
            SizeOf(world, content, character)?.MaxPositionDepth;

        /// <summary>Baja el cargo hasta el más alto que existe en una empresa con ese tope.</summary>
        public static CareerPositionDefinition Cap(CareerPositionDefinition position, int? maxDepth)
        {
            while (maxDepth.HasValue && position.Depth > maxDepth.Value && position.Previous != null)
            {
                position = position.Previous;
            }

            return position;
        }

        /// <summary>
        /// Está en el cargo más alto de su empresa, aunque en una empresa más grande
        /// podría subir más.
        /// </summary>
        public static bool AtCompanyTop(World world, ContentDatabase content, Character character)
        {
            CareerPosition? position = character.Job?.Position;
            int? max = MaxDepth(world, content, character);
            if (position == null || !max.HasValue)
            {
                return false;
            }

            CareerPositionDefinition definition = content.GetCareerPosition(position.PositionId);
            return !definition.IsTop && definition.Depth >= max.Value;
        }
    }
}

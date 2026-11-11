using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Scripting;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Reglas de los servicios que contrata el jugador ("economia.servicios"):
    /// contratar una opción (reemplaza la que tenía de ese servicio), cancelarla y
    /// saber cuánto cuesta. El cobro de cada mes lo hace <see cref="ExpenseSystem"/>.
    /// </summary>
    public static class ServiceRules
    {
        /// <summary>Los servicios del país donde vive (vacío si no vive en ninguna parte).</summary>
        public static IReadOnlyList<ServiceDefinition> ServicesFor(ContentDatabase content, Character character) =>
            character.Residence == null
                ? new List<ServiceDefinition>()
                : (IReadOnlyList<ServiceDefinition>?)content.GetCountry(character.Residence.CountryId).Economy?.Services ?? new List<ServiceDefinition>();

        /// <summary>El servicio de su país que tiene esa opción, o null.</summary>
        public static ServiceDefinition? ServiceOf(ContentDatabase content, Character character, string optionId) =>
            ServicesFor(content, character).FirstOrDefault(s => s.Option(optionId) != null);

        /// <summary>Lo que cuesta al mes esa opción, en dinero del inicio del mundo.</summary>
        public static decimal Price(ContentDatabase content, Character character, string optionId, string where)
        {
            ServiceDefinition service = ServiceOf(content, character, optionId)
                ?? throw new ScriptException($"{where}('{optionId}'): ningún servicio de su país tiene esa opción.");
            return service.Option(optionId)!.Price;
        }

        /// <summary>El jugador contrata esa opción; si ya tenía otra del mismo servicio, la cambia.</summary>
        public static void Contract(ContentDatabase content, Character character, string optionId)
        {
            ServiceDefinition service = ServiceOf(content, character, optionId)
                ?? throw new ScriptException($"jugador.contratar('{optionId}'): ningún servicio de su país tiene esa opción.");
            character.Services[service.Id] = optionId;
        }

        /// <summary>Deja de pagar ese servicio desde el próximo mes.</summary>
        public static void Cancel(Character character, string serviceId) => character.Services.Remove(serviceId);
    }
}

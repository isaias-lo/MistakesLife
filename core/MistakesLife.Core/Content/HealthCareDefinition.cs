using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.salud" de country.json: el sistema de salud del país. Hay
    /// una previsión pública (en Chile, Fonasa) con tramos según lo que gana la
    /// persona, y planes privados (las isapres) que el jugador puede contratar. El
    /// plan privado cuesta su precio base por el factor de la edad, y se paga con
    /// la cotización de salud de la liquidación (si el plan cuesta más, se descuenta
    /// la diferencia). También dice cuánto cuesta cada atención (consulta, urgencia)
    /// según la previsión.
    /// </summary>
    public sealed class HealthCareDefinition
    {
        /// <summary>Id de la previsión pública (por ejemplo "prevision.chile.fonasa"): la que tiene todo el que no contrató un plan.</summary>
        [JsonProperty("publico", Required = Required.Always)]
        public string PublicId { get; set; } = string.Empty;

        /// <summary>Tramo de la previsión pública para quien no tiene ingresos (en Chile, el tramo A).</summary>
        [JsonProperty("sin_ingresos", Required = Required.Always)]
        public string NoIncomeTierId { get; set; } = string.Empty;

        /// <summary>Tramos de la previsión pública según lo que gana, de menor a mayor; el último no tiene tope.</summary>
        [JsonProperty("tramos", Required = Required.Always)]
        public List<HealthTierDefinition> Tiers { get; set; } = new List<HealthTierDefinition>();

        /// <summary>Id del descuento de la liquidación que es la cotización de salud (el 7 % en Chile).</summary>
        [JsonProperty("descuento", Required = Required.Always)]
        public string DeductionId { get; set; } = string.Empty;

        /// <summary>Id de la línea de la liquidación con lo que el plan privado cuesta más que la cotización.</summary>
        [JsonProperty("descuento_adicional", Required = Required.Always)]
        public string ExtraDeductionId { get; set; } = string.Empty;

        /// <summary>
        /// Parte del sueldo bruto de honorarios que ya va a salud con la retención (en
        /// Chile, el 7 % del 80 % de lo boleteado: 0,056). Quien trabaja a honorarios
        /// con un plan privado paga aparte solo lo que el plan cuesta más que eso.
        /// </summary>
        [JsonProperty("cotizacion_honorarios")]
        public double FreelanceRate { get; set; }

        /// <summary>Planes privados que se pueden contratar.</summary>
        [JsonProperty("planes")]
        public List<HealthPlanDefinition> Plans { get; set; } = new List<HealthPlanDefinition>();

        /// <summary>Factor del precio de un plan según la edad (tabla de factores), de menor a mayor edad.</summary>
        [JsonProperty("factores_edad")]
        public List<HealthAgeFactorDefinition> AgeFactors { get; set; } = new List<HealthAgeFactorDefinition>();

        /// <summary>Las atenciones de salud y lo que cuestan (consulta, urgencia...).</summary>
        [JsonProperty("atenciones")]
        public List<HealthCareServiceDefinition> Services { get; set; } = new List<HealthCareServiceDefinition>();

        /// <summary>El plan privado con ese id, o null.</summary>
        public HealthPlanDefinition? Plan(string id) => Plans.FirstOrDefault(p => p.Id == id);

        /// <summary>La atención con ese id, o null.</summary>
        public HealthCareServiceDefinition? Service(string id) => Services.FirstOrDefault(s => s.Id == id);

        /// <summary>El factor de la tabla para esa edad (1 si no hay tabla).</summary>
        public double AgeFactor(int age)
        {
            double factor = 1;
            foreach (HealthAgeFactorDefinition band in AgeFactors)
            {
                if (age >= band.FromAge)
                {
                    factor = band.Factor;
                }
            }

            return factor;
        }
    }
}

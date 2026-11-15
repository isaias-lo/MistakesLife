using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Lo que hace de una carrera un oficio (el bloque "oficio" de career.json): se trabaja
    /// sin título, el sueldo parte del sueldo mínimo, hay una meta cada mes y una parte del
    /// pago es variable (comisión, propinas o trato). Ver <see cref="Work.OccupationRules"/>.
    /// </summary>
    public sealed class OccupationDefinition
    {
        /// <summary>Formas de pago variable que entiende el núcleo.</summary>
        public const string Commission = "comision";
        public const string Tips = "propina";
        public const string PieceRate = "trato";

        public static readonly string[] VariableKinds = { Commission, Tips, PieceRate };

        /// <summary>Sueldo base del primer cargo, en veces el sueldo mínimo del país. Si falta, 1.</summary>
        [JsonProperty("sueldo_base")]
        public double BaseSalary { get; set; } = 1;

        /// <summary>
        /// El sueldo base en un país donde el oficio paga distinto frente a su sueldo mínimo
        /// (en Argentina los convenios de comercio, gastronomía y construcción pagan varias
        /// veces el salario mínimo). Por id de país; opcional.
        /// </summary>
        [JsonProperty("sueldo_base_por_pais")]
        public Dictionary<string, double> BaseSalaryByCountry { get; set; } = new Dictionary<string, double>();

        /// <summary>El sueldo base en ese país (veces su sueldo mínimo).</summary>
        public double BaseSalaryIn(string countryId) => BaseSalaryByCountry.TryGetValue(countryId, out double value) ? value : BaseSalary;

        /// <summary>
        /// Cómo es la parte variable: "comision" y "trato" entran en la liquidación (son
        /// imponibles); "propina" la pagan los clientes y no es sueldo. Opcional.
        /// </summary>
        [JsonProperty("pago_variable")]
        public string? VariableKind { get; set; }

        /// <summary>
        /// Cuánto es lo variable en un mes en que cumple justo la meta, frente al sueldo
        /// (0.5 = la mitad del sueldo). Si falta, 0.
        /// </summary>
        [JsonProperty("variable_tipico")]
        public double TypicalVariable { get; set; }

        /// <summary>
        /// Cuánto se mueve el trabajo cada mes del año, de enero a diciembre (1 = un mes
        /// normal): diciembre en el comercio, el invierno lluvioso en la construcción. Cambia
        /// lo variable que se gana, no la meta (la meta ya viene pensada para cada mes).
        /// Opcional: sin ella, todos los meses valen 1.
        /// </summary>
        [JsonProperty("temporada")]
        public List<double> Season { get; set; } = new List<double>();

        /// <summary>
        /// Clave del nombre de la jefatura (por ejemplo "oficio.garzon.jefatura"): sus textos
        /// son la clave con ".f" y ".m". Opcional: sin ella, "jefa" o "jefe".
        /// </summary>
        [JsonProperty("jefatura")]
        public string? BossKey { get; set; }

        /// <summary>Cuánto vale ese mes (1 a 12) en la temporada.</summary>
        public double SeasonOf(int month) => Season.Count == 12 ? Season[month - 1] : 1;
    }
}

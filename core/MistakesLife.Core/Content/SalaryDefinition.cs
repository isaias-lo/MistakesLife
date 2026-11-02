using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.sueldos" de country.json: sueldos de referencia del país,
    /// mensuales y brutos, en dinero de cuando empieza el mundo.
    /// </summary>
    public sealed class SalaryDefinition
    {
        /// <summary>Sueldo mínimo legal mensual (bruto).</summary>
        [JsonProperty("minimo", Required = Required.Always)]
        public decimal Minimum { get; set; }

        /// <summary>Sueldo típico (la mediana) mensual bruto de quienes trabajan.</summary>
        [JsonProperty("tipico", Required = Required.Always)]
        public decimal Typical { get; set; }

        /// <summary>
        /// Cuánto gana alguien sin título frente al sueldo típico (0.85 = 85 %).
        /// Los titulados usan el "sueldo_relativo" del nivel de su carrera.
        /// </summary>
        [JsonProperty("relativo_sin_titulo")]
        public double NoDegreeRelative { get; set; } = 1;

        /// <summary>
        /// Cada cuántos meses se reajustan los sueldos según la inflación. Entre
        /// reajustes el sueldo pierde poder de compra: con inflación alta se nota mucho.
        /// </summary>
        [JsonProperty("reajuste_meses")]
        public int AdjustmentMonths { get; set; } = 12;
    }
}

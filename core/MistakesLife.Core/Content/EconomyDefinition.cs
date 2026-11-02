using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia" de country.json: cómo funciona el dinero en ese país.
    /// Por ahora, la inflación y los sueldos; después se suman gastos, deudas y pensiones.
    /// </summary>
    public sealed class EconomyDefinition
    {
        /// <summary>Cómo genera el juego la inflación del país.</summary>
        [JsonProperty("inflacion", Required = Required.Always)]
        public InflationDefinition Inflation { get; set; } = new InflationDefinition();

        /// <summary>Sueldos de referencia (mínimo y típico). Si falta, nadie gana sueldo en este país.</summary>
        [JsonProperty("sueldos")]
        public SalaryDefinition? Salaries { get; set; }

        /// <summary>Descuentos e impuesto de la liquidación de sueldo. Si falta, el sueldo llega entero.</summary>
        [JsonProperty("liquidacion")]
        public PayrollDefinition Payroll { get; set; } = new PayrollDefinition();

        /// <summary>Gastos de cada mes del hogar. Si falta, no hay gastos.</summary>
        [JsonProperty("gastos")]
        public ExpensesDefinition? Expenses { get; set; }

        /// <summary>La declaración anual de impuestos. Si falta, nadie declara.</summary>
        [JsonProperty("declaracion_anual")]
        public AnnualTaxDefinition? AnnualTax { get; set; }
    }
}

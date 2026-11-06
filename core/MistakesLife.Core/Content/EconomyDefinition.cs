using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia" de country.json: cómo funciona el dinero en ese país.
    /// Inflación, sueldos, impuestos, gastos, pensiones y deudas.
    /// </summary>
    public sealed class EconomyDefinition
    {
        /// <summary>Cómo genera el juego la inflación del país.</summary>
        [JsonProperty("inflacion", Required = Required.Always)]
        public InflationDefinition Inflation { get; set; } = new InflationDefinition();

        /// <summary>El ciclo económico (normal, recesión, crisis). Si falta, el país no tiene crisis.</summary>
        [JsonProperty("ciclo")]
        public EconomicCycleDefinition? Cycle { get; set; }

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

        /// <summary>Pensiones: cuenta individual y pensión del Estado. Si falta, nadie recibe pensión.</summary>
        [JsonProperty("jubilacion")]
        public PensionsDefinition? Pensions { get; set; }

        /// <summary>Tipos de deuda y registro de morosos. Si falta, nadie puede endeudarse.</summary>
        [JsonProperty("deudas")]
        public DebtsDefinition? Debts { get; set; }

        /// <summary>Herencia de bienes y deudas al morir. Si falta, lo que deja se pierde.</summary>
        [JsonProperty("herencia")]
        public InheritanceDefinition? Inheritance { get; set; }

        /// <summary>Tamaños de empresa y cómo viven. Si falta, los trabajos no tienen empresa.</summary>
        [JsonProperty("empresas")]
        public CompaniesDefinition? Companies { get; set; }
    }
}

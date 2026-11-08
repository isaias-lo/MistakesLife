using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Jubilación de reparto (campo "economia.jubilacion.reparto"), como la de
    /// Argentina: quien trabaja con contrato aporta cada mes, pero el aporte no se
    /// guarda en una cuenta propia. Al jubilarse, el Estado paga una pensión según
    /// los años de aportes y el sueldo de los últimos años:
    /// base_universal + porcentaje_por_ano × años × sueldo promedio,
    /// y nunca menos que la mínima. Sin los años mínimos no hay pensión propia
    /// (queda la pensión universal del Estado, si el país la tiene).
    /// </summary>
    public sealed class PayAsYouGoPensionDefinition
    {
        /// <summary>El descuento de la liquidación que es el aporte jubilatorio (por ejemplo "descuento.argentina.jubilacion").</summary>
        [JsonProperty("descuento", Required = Required.Always)]
        public string Deduction { get; set; } = string.Empty;

        /// <summary>Años de aportes que pide la ley para jubilarse con pensión propia.</summary>
        [JsonProperty("anos_minimos")]
        public int MinimumYears { get; set; } = 30;

        /// <summary>Parte del sueldo promedio que suma cada año de aportes (por ejemplo 0,015).</summary>
        [JsonProperty("porcentaje_por_ano")]
        public double SharePerYear { get; set; }

        /// <summary>Cuántos años de sueldo se promedian (los últimos).</summary>
        [JsonProperty("anos_promedio")]
        public int AverageYears { get; set; } = 10;

        /// <summary>Parte fija que recibe todo jubilado con los años mínimos, en dinero del inicio del mundo.</summary>
        [JsonProperty("base_universal")]
        public decimal UniversalBase { get; set; }

        /// <summary>La pensión más baja que se paga a quien cumple los años mínimos, en dinero del inicio del mundo.</summary>
        [JsonProperty("minima")]
        public decimal Minimum { get; set; }
    }
}

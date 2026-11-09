using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.gastos" de country.json: los gastos de cada mes del
    /// hogar (arriendo, comida, cuentas, transporte, hijos…).
    /// </summary>
    public sealed class ExpensesDefinition
    {
        /// <summary>Edad desde la que el personaje paga sus gastos (antes, los pagan sus padres).</summary>
        [JsonProperty("edad_desde")]
        public int FromAge { get; set; } = 18;

        /// <summary>Felicidad que pierde cada mes que termina con el dinero en negativo (estrés financiero).</summary>
        [JsonProperty("felicidad_en_rojo")]
        public int HappinessInRed { get; set; }

        /// <summary>
        /// Deuda (de "economia.deudas") que cubre lo que falta cuando los gastos del
        /// mes son más que el dinero: la línea de crédito o el sobregiro del banco.
        /// Sin ella, el dinero queda en negativo.
        /// </summary>
        [JsonProperty("deuda_por_deficit")]
        public string? DeficitDebt { get; set; }

        /// <summary>
        /// Hasta cuánto llega esa deuda (el cupo de la línea, en dinero del inicio
        /// del mundo). Lo que falta por sobre el cupo no se paga: son cuentas que
        /// quedan atrasadas y meses en rojo. Sin cupo, la deuda cubre todo.
        /// </summary>
        [JsonProperty("cupo_deficit")]
        public decimal? DeficitLimit { get; set; }

        [JsonProperty("categorias", Required = Required.Always)]
        public List<ExpenseDefinition> Categories { get; set; } = new List<ExpenseDefinition>();
    }
}

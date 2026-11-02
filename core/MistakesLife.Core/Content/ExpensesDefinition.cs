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

        [JsonProperty("categorias", Required = Required.Always)]
        public List<ExpenseDefinition> Categories { get; set; } = new List<ExpenseDefinition>();
    }
}

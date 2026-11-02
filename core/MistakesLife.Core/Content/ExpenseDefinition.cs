using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un gasto mensual, por ejemplo el arriendo. Su id (que empieza con "gasto.")
    /// es también la clave de su nombre en lang/.
    /// </summary>
    public sealed class ExpenseDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Cuánto cuesta al mes, en dinero del inicio del mundo. Es una expresión del
        /// lenguaje: "200000 + 90000 * jugador.hijos_menores" o "arriendo()".
        /// </summary>
        [JsonProperty("monto", Required = Required.Always)]
        public string Amount { get; set; } = string.Empty;

        /// <summary>Cuándo se paga (por ejemplo, solo si vive independiente). Si falta, siempre.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>Si se divide en dos cuando el personaje tiene pareja (como el arriendo o la comida de la casa).</summary>
        [JsonProperty("compartido")]
        public bool Shared { get; set; }

        [JsonIgnore]
        public Expression? CompiledAmount { get; set; }

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }
    }
}

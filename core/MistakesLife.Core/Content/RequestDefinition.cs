using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un momento que pide el jugador (el bloque "a_pedido" de un evento): no pasa solo, lo
    /// empieza el jugador con un botón cuando quiere (pedir un aumento, tomar vacaciones,
    /// renunciar). Es la iniciativa del rediseño: no esperar a que la vida te llame.
    /// </summary>
    public sealed class RequestDefinition
    {
        /// <summary>Clave del texto del botón ("accion.pega.pedir_aumento").</summary>
        [JsonProperty("texto", Required = Required.Always)]
        public string TextKey { get; set; } = string.Empty;

        /// <summary>
        /// Cuántas veces al año lo pediría alguien con esta personalidad, para el simulador de
        /// vidas (que no tiene a nadie apretando botones). Una expresión: "0.3 + jugador.extraversion / 200".
        /// </summary>
        [JsonProperty("por_ano", Required = Required.Always)]
        public string PerYear { get; set; } = "0";

        [JsonIgnore]
        internal Expression? CompiledPerYear { get; set; }

        /// <summary>Cuántas veces al año lo pediría esa persona (0 si no se puede calcular).</summary>
        public double PerYearFor(IScriptScope scope) => CompiledPerYear?.Evaluate(scope).AsNumber("a_pedido.por_ano") ?? 0;
    }
}

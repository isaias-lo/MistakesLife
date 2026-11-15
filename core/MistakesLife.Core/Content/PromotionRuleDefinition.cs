using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una forma de pasar de curso: con hasta "reprobados" ramos bajo la nota para aprobar,
    /// si el promedio general llega a "promedio_minimo" (sin mínimo si falta). En Chile
    /// (Decreto 67 de 2018): sin rojos; con uno y promedio 4,5; con dos y promedio 5,0.
    /// </summary>
    public sealed class PromotionRuleDefinition
    {
        [JsonProperty("reprobados", Required = Required.Always)]
        public int Failed { get; set; }

        [JsonProperty("promedio_minimo")]
        public double? MinAverage { get; set; }
    }
}

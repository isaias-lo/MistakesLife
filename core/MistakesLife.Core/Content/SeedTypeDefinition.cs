using System;
using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Time;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tipo de semilla tal como se escribe en un paquete JSON (carpeta seeds/).
    /// </summary>
    public sealed class SeedTypeDefinition
    {
        /// <summary>Id estable, por ejemplo "semilla.copio_paes".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>"positiva", "negativa" o "ambigua".</summary>
        [JsonProperty("tipo", Required = Required.Always)]
        public string Polarity { get; set; } = string.Empty;

        /// <summary>Fuerza inicial de 0 a 100.</summary>
        [JsonProperty("fuerza")]
        public int Strength { get; set; } = 50;

        /// <summary>Años que puede dormir antes de olvidarse.</summary>
        [JsonProperty("vence_anios")]
        public int ExpiresAfterYears { get; set; } = 20;

        /// <summary>Semanas mínimas dormida antes de poder despertar.</summary>
        [JsonProperty("dormir_min_semanas")]
        public int MinWeeksDormant { get; set; }

        /// <summary>Condición para despertar (lenguaje de condiciones). Opcional.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        [JsonProperty("heredable")]
        public bool Heritable { get; set; }

        /// <summary>Paquete del que viene (lo llena el cargador).</summary>
        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        /// <summary>Ids de los eventos que esta semilla puede generar (los calcula la base de contenido).</summary>
        [JsonIgnore]
        public List<string> PossibleEvents { get; } = new List<string>();

        public SeedPolarity ParsePolarity()
        {
            switch (Polarity)
            {
                case "positiva":
                    return SeedPolarity.Positive;
                case "negativa":
                    return SeedPolarity.Negative;
                case "ambigua":
                    return SeedPolarity.Ambiguous;
                default:
                    throw new FormatException($"Tipo de semilla desconocido '{Polarity}' (usa positiva, negativa o ambigua).");
            }
        }

        /// <summary>Convierte el dato del paquete en lo que usa el sistema de semillas.</summary>
        public SeedDefinition ToSeedDefinition(ContentDatabase content)
        {
            var definition = new SeedDefinition(Id, ParsePolarity(), Strength, ExpiresAfterYears * GameDate.WeeksPerYear)
            {
                MinWeeksDormant = MinWeeksDormant,
                Heritable = Heritable,
                Condition = CompiledConditions == null ? null : new Events.ExpressionSeedCondition(CompiledConditions, content),
            };
            foreach (string eventId in PossibleEvents)
            {
                definition.PossibleEvents.Add(eventId);
            }

            return definition;
        }
    }
}

using System;
using System.Collections.Generic;

namespace MistakesLife.Core.Seeds
{
    /// <summary>
    /// Los datos con que se planta una semilla. Más adelante vendrán de los
    /// paquetes JSON; el núcleo solo los usa.
    /// </summary>
    public sealed class SeedDefinition
    {
        public SeedDefinition(string typeId, SeedPolarity polarity, int strength, int expiresAfterWeeks)
        {
            if (string.IsNullOrWhiteSpace(typeId))
            {
                throw new ArgumentException("La semilla necesita un id de tipo.", nameof(typeId));
            }

            if (expiresAfterWeeks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expiresAfterWeeks), "Debe ser mayor que 0.");
            }

            TypeId = typeId;
            Polarity = polarity;
            Strength = strength;
            ExpiresAfterWeeks = expiresAfterWeeks;
        }

        /// <summary>Id estable del tipo de semilla, por ejemplo "semilla.copio_paes".</summary>
        public string TypeId { get; }

        public SeedPolarity Polarity { get; }

        /// <summary>Fuerza inicial, de 0 a 100: qué tan probable es que vuelva.</summary>
        public int Strength { get; }

        /// <summary>Cuántas semanas puede dormir antes de olvidarse.</summary>
        public int ExpiresAfterWeeks { get; }

        /// <summary>Semanas mínimas que duerme antes de poder despertar.</summary>
        public int MinWeeksDormant { get; set; }

        /// <summary>Condición para despertar. Si no hay, puede despertar en cualquier momento.</summary>
        public ISeedCondition? Condition { get; set; }

        /// <summary>Ids de los eventos que puede generar al despertar.</summary>
        public IList<string> PossibleEvents { get; } = new List<string>();

        /// <summary>Si pasa directo al hijo cuando el dueño muere (legado).</summary>
        public bool Heritable { get; set; }
    }
}

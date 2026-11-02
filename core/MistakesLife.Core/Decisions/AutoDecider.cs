using System;
using System.Collections.Generic;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Decisions
{
    /// <summary>
    /// Decide por un personaje que no maneja el jugador (el simulador hoy; los personajes
    /// del mundo más adelante). Cada opción pesa según su "tendencia", que el paquete
    /// calcula con la personalidad, el hogar y la historia del personaje. No es
    /// determinista: dos personas parecidas pueden elegir distinto, pero la tendencia manda.
    /// </summary>
    public sealed class AutoDecider
    {
        /// <summary>Peso de una opción sin tendencia escrita.</summary>
        public const double DefaultTendency = 50;

        private readonly ContentDatabase _content;

        public AutoDecider(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>Elige una de las opciones disponibles del evento pendiente.</summary>
        public int Choose(World world, PendingEvent pending, GameRandom random)
        {
            IReadOnlyList<double> weights = Weights(world, pending);
            double total = 0;
            foreach (double w in weights)
            {
                total += w;
            }

            if (total <= 0)
            {
                return random.Pick(pending.AvailableOptions);
            }

            double roll = random.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    return pending.AvailableOptions[i];
                }
            }

            return pending.AvailableOptions[pending.AvailableOptions.Count - 1];
        }

        /// <summary>El peso de cada opción disponible, en el mismo orden que AvailableOptions.</summary>
        public IReadOnlyList<double> Weights(World world, PendingEvent pending)
        {
            EventDefinition definition = _content.GetEvent(pending.EventId);
            Character subject = world.GetCharacter(pending.CharacterId);
            Seed? seed = pending.SeedId.HasValue ? world.Seeds.Get(pending.SeedId.Value) : null;
            var scope = new EventScope(world, _content, subject, seed, readOnly: true);

            var weights = new List<double>(pending.AvailableOptions.Count);
            foreach (int index in pending.AvailableOptions)
            {
                Expression? tendency = definition.Options[index].CompiledTendency;
                double weight = tendency == null ? DefaultTendency : tendency.Evaluate(scope).AsNumber($"tendencia de {definition.Id}, opción {index + 1}");
                weights.Add(Math.Max(0, weight));
            }

            return weights;
        }
    }
}

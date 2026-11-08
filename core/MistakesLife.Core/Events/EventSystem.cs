using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Fase de eventos del ciclo semanal. Primero convierte en escena las semillas
    /// que despertaron; si no hay, prueba los eventos con probabilidad anual
    /// (cada uno con su propia oportunidad) y luego, con cierta probabilidad,
    /// elige un evento al azar según sus condiciones y su peso. Un día clave detiene el tiempo.
    /// Mientras el jugador tenga un evento sin responder, no ocurren otros.
    /// </summary>
    public sealed class EventSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.eventos";

        public EventSystem(ContentDatabase content)
            : this(content, new EventRules())
        {
        }

        public EventSystem(ContentDatabase content, EventRules rules)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public ContentDatabase Content { get; }

        public EventRules Rules { get; }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Events;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || world.PendingEvents.Any(p => p.CharacterId == player.Id))
            {
                return;
            }

            GameRandom random = context.RandomFor(Id);

            foreach (Seed seed in world.Seeds.Awake.Where(s => s.OwnerId == player.Id).ToList())
            {
                var scope = new EventScope(world, Content, player, seed, readOnly: true);
                List<EventDefinition> candidates = Content.Events
                    .Where(e => e.SeedType == seed.TypeId && Passes(e, scope))
                    .ToList();
                if (candidates.Count > 0)
                {
                    Occur(world, context, PickWeighted(candidates, random), player, seed);
                    return;
                }
            }

            // Eventos con probabilidad anual: cada uno tiene su propia oportunidad cada semana.
            var randomScope = new EventScope(world, Content, player, null, readOnly: true);
            foreach (EventDefinition ev in Content.Events.Where(e => e.AnnualProbability.HasValue))
            {
                if (CanHappen(world, ev, player, randomScope) && random.Chance(ev.WeeklyProbability))
                {
                    Occur(world, context, ev, player, null);
                    return;
                }
            }

            if (!random.Chance(Rules.WeeklyRandomEventChance))
            {
                return;
            }

            List<EventDefinition> pool = Content.Events
                .Where(e => e.SeedType == null && !e.AnnualProbability.HasValue && CanHappen(world, e, player, randomScope))
                .ToList();
            if (pool.Count > 0)
            {
                Occur(world, context, PickWeighted(pool, random), player, null);
            }
        }

        /// <summary>
        /// El jugador elige una opción de un evento pendiente: se aplican sus efectos,
        /// se anota en el historial y, si lo provocó una semilla, la semilla se cierra.
        /// </summary>
        public void Choose(World world, PendingEvent pending, int optionIndex)
        {
            if (!world.PendingEvents.Contains(pending))
            {
                throw new InvalidOperationException("Ese evento no está pendiente.");
            }

            if (!pending.AvailableOptions.Contains(optionIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(optionIndex), "Esa opción no está disponible.");
            }

            EventDefinition definition = Content.GetEvent(pending.EventId);
            Character subject = world.GetCharacter(pending.CharacterId);
            Seed? seed = pending.SeedId.HasValue ? world.Seeds.Get(pending.SeedId.Value) : null;
            var scope = new EventScope(world, Content, subject, seed, readOnly: false);

            foreach (var effect in definition.Options[optionIndex].CompiledEffects)
            {
                effect.Apply(scope);
            }

            world.RemovePendingEvent(pending);
            Finish(world, definition, subject, seed, optionIndex);
        }

        private void Occur(World world, WeekContext context, EventDefinition definition, Character subject, Seed? seed)
        {
            var scope = new EventScope(world, Content, subject, seed, readOnly: false);
            foreach (var effect in definition.CompiledEffects)
            {
                effect.Apply(scope);
            }

            var readScope = new EventScope(world, Content, subject, seed, readOnly: true);
            var options = new List<int>();
            for (int i = 0; i < definition.Options.Count; i++)
            {
                var condition = definition.Options[i].CompiledConditions;
                if (condition == null || condition.IsTrue(readScope))
                {
                    options.Add(i);
                }
            }

            if (definition.KeyDay)
            {
                context.RequestStop(definition.Id);
            }

            if (options.Count == 0)
            {
                Finish(world, definition, subject, seed, null);
                return;
            }

            world.AddPendingEvent(new PendingEvent(definition.Id, subject.Id, world.Date, seed?.Id, options));
        }

        private static void Finish(World world, EventDefinition definition, Character subject, Seed? seed, int? option)
        {
            world.RecordEvent(new EventRecord(world.Date, definition.Id, subject.Id, option));
            if (seed != null && seed.IsOpen)
            {
                world.Seeds.Resolve(seed, world.Date);
            }
        }

        private static bool CanHappen(World world, EventDefinition definition, Character player, EventScope scope) =>
            !(definition.Unique && HasHappened(world, definition.Id, player.Id)) && !InCooldown(world, definition, player.Id) && Passes(definition, scope);

        private static bool Passes(EventDefinition definition, EventScope scope) =>
            (definition.CountryId == null || scope.Subject.Residence?.CountryId == definition.CountryId)
            && (definition.CompiledConditions == null || definition.CompiledConditions.IsTrue(scope));

        private static bool HasHappened(World world, string eventId, int characterId) =>
            world.EventHistory.Any(r => r.EventId == eventId && r.CharacterId == characterId);

        private static bool InCooldown(World world, EventDefinition definition, int characterId)
        {
            if (definition.CooldownWeeks <= 0)
            {
                return false;
            }

            for (int i = world.EventHistory.Count - 1; i >= 0; i--)
            {
                EventRecord record = world.EventHistory[i];
                if (record.EventId == definition.Id && record.CharacterId == characterId)
                {
                    return world.Date.WeeksSince(record.Date) < definition.CooldownWeeks;
                }
            }

            return false;
        }

        private static EventDefinition PickWeighted(List<EventDefinition> candidates, GameRandom random)
        {
            int total = candidates.Sum(c => c.Weight);
            int roll = random.NextInt(total);
            foreach (EventDefinition candidate in candidates)
            {
                roll -= candidate.Weight;
                if (roll < 0)
                {
                    return candidate;
                }
            }

            return candidates[candidates.Count - 1];
        }
    }
}

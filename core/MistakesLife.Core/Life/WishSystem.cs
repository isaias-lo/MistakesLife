using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Lo que quiero (rediseño R2c). En cada etapa del horario (cada año escolar, la
    /// universidad, un trabajo) el jugador quiere 2 o 3 cosas que nacen de su vida.
    /// Cada mes se revisa si se cumplieron; al cambiar de etapa, lo que perseguía y no
    /// pasó queda como perdido en el diario, y lo demás se le pasa sin pena.
    /// </summary>
    public sealed class WishSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.deseos";

        /// <summary>Cuántas cosas quiere a la vez, como mucho, en una etapa.</summary>
        public const int MaxPerStage = 3;

        /// <summary>Ánimo al cumplirse algo que quería (el doble si lo perseguía).</summary>
        public const int FulfilledHappiness = 3;

        /// <summary>Ánimo que se pierde cuando se acaba la etapa sin lograr lo que perseguía.</summary>
        public const int LostHappiness = 3;

        /// <summary>Semanas antes de que se pueda volver a querer lo mismo (más de un año escolar).</summary>
        public const int RestWeeks = 60;

        private readonly ContentDatabase _content;

        public WishSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Consequences;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.Routine == null)
            {
                return;
            }

            string stage = player.Routine.StageKey;
            foreach (Wish wish in player.Wishes.Where(w => w.State == WishState.Open && w.StageKey != stage).ToList())
            {
                Close(world, player, wish);
            }

            if (!player.Wishes.Any(w => w.StageKey == stage))
            {
                Choose(world, player, stage, context.RandomFor(Id));
            }

            if (!context.Date.IsFirstWeekOfMonth)
            {
                return;
            }

            foreach (Wish wish in player.Wishes.Where(w => w.State == WishState.Open).ToList())
            {
                WishDefinition? definition = _content.Wish(wish.WishId);
                Character? person = PersonOf(world, wish);
                if (definition == null || (wish.PersonId.HasValue && (person == null || !person.IsAlive)))
                {
                    wish.State = WishState.Faded;
                    wish.Closed = world.Date;
                    continue;
                }

                if (IsTrue(definition.CompiledFulfilled, world, player, person))
                {
                    wish.State = WishState.Done;
                    wish.Closed = world.Date;
                    player.Stats.Happiness += wish.Highlighted ? 2 * FulfilledHappiness : FulfilledHappiness;
                    world.WriteJournal(player.Id, definition.FulfilledJournalKey);
                }
            }
        }

        /// <summary>La persona de un deseo, si es con alguien y sigue en el mundo.</summary>
        public static Character? PersonOf(World world, Wish wish) =>
            wish.PersonId.HasValue && world.TryGetCharacter(wish.PersonId.Value, out Character? person) ? person : null;

        private void Close(World world, Character player, Wish wish)
        {
            wish.Closed = world.Date;
            if (!wish.Highlighted)
            {
                wish.State = WishState.Faded;
                return;
            }

            wish.State = WishState.Lost;
            player.Stats.Happiness -= LostHappiness;
            WishDefinition? definition = _content.Wish(wish.WishId);
            if (definition != null)
            {
                world.WriteJournal(player.Id, definition.LostJournalKey);
            }
        }

        private void Choose(World world, Character player, string stage, GameRandom random)
        {
            int age = player.AgeInYears(world.Date);
            var candidates = new List<(WishDefinition Definition, Character? Person)>();
            foreach (WishDefinition definition in _content.Wishes)
            {
                if (age < definition.MinAge || age > definition.MaxAge)
                {
                    continue;
                }

                if (!definition.Repeatable && player.Wishes.Any(w => w.WishId == definition.Id && w.State == WishState.Done))
                {
                    continue;
                }

                // Regla de repeticiones: lo mismo no se quiere dos etapas seguidas (un año sí, otro no).
                if (player.Wishes.Any(w => w.WishId == definition.Id && world.Date.WeeksSince(w.Since) < RestWeeks))
                {
                    continue;
                }

                Character? person = null;
                if (definition.With != null)
                {
                    IReadOnlyList<Character> people = ChatSystem.PeopleAs(world, player, definition.With, minAge: 0);
                    if (people.Count == 0)
                    {
                        continue;
                    }

                    person = people[random.NextInt(people.Count)];
                }

                // Nace de su vida (condiciones) y todavía no lo tiene (si ya está cumplido, no se desea).
                if (IsTrue(definition.CompiledConditions, world, player, person, whenMissing: true)
                    && !IsTrue(definition.CompiledFulfilled, world, player, person))
                {
                    candidates.Add((definition, person));
                }
            }

            for (int i = 0; i < MaxPerStage && candidates.Count > 0; i++)
            {
                int total = candidates.Sum(c => c.Definition.Weight);
                int roll = random.NextInt(total);
                int picked = 0;
                while (roll >= candidates[picked].Definition.Weight)
                {
                    roll -= candidates[picked].Definition.Weight;
                    picked++;
                }

                (WishDefinition definition, Character? person) = candidates[picked];
                player.Wishes.Add(new Wish(definition.Id, stage, world.Date, person?.Id));
                candidates.RemoveAt(picked);
            }
        }

        private bool IsTrue(Scripting.Expression? expression, World world, Character player, Character? person, bool whenMissing = false)
        {
            if (expression == null)
            {
                return whenMissing;
            }

            var scope = new EventScope(world, _content, player, null, readOnly: true, person);
            return expression.IsTrue(scope);
        }
    }
}

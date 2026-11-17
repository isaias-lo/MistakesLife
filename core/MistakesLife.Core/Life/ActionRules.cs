using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Events;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Lo que el jugador hace con el tiempo parado (acciones/ en los paquetes, D3): cada acción
    /// gasta tardes libres del mes (<see cref="FreeTimeRules"/>) y da su resultado al tiro, sin
    /// preguntas: las barras suben o bajan, la persona lo quiere más, la prueba queda mejor
    /// preparada. Las reglas de cada acción (con quién, cuántas veces al mes, desde qué edad, qué
    /// cambia) están en su paquete.
    /// </summary>
    public static class ActionRules
    {
        /// <summary>
        /// Lo que puede hacer ahora en ese grupo (o en todos, si es null): las acciones cuya edad y
        /// condiciones cumple, una por persona si son con alguien, que no hizo ya las veces del mes.
        /// Las que cuestan más tardes de las que le quedan también salen: <see cref="CanAfford"/> dice si alcanza.
        /// </summary>
        public static IReadOnlyList<ActionOption> Available(World world, ContentDatabase content, Character character, string? group)
        {
            var options = new List<ActionOption>();
            if (!character.IsAlive || character.Routine == null)
            {
                return options;
            }

            int age = character.AgeInYears(world.Date);
            FreeTime? month = FreeTimeRules.ThisMonth(world, character);
            var scope = new EventScope(world, content, character, null, readOnly: true);
            foreach (ActionDefinition action in content.Actions)
            {
                if ((group != null && action.Group != group) || age < action.MinAge || age > action.MaxAge
                    || (action.CompiledConditions != null && !action.CompiledConditions.Evaluate(scope).AsBool(action.Id)))
                {
                    continue;
                }

                if (action.With == null)
                {
                    if (TimesThisMonth(month, DoneKey(action, null)) < action.PerMonth)
                    {
                        options.Add(new ActionOption(action, null));
                    }

                    continue;
                }

                foreach (Character person in ChatSystem.PeopleAs(world, character, action.With, 0).OrderBy(p => p.Id))
                {
                    if (TimesThisMonth(month, DoneKey(action, person)) < action.PerMonth)
                    {
                        options.Add(new ActionOption(action, person));
                    }
                }
            }

            return options;
        }

        /// <summary>Si le quedan tardes libres para hacerla este mes.</summary>
        public static bool CanAfford(World world, ContentDatabase content, Character character, ActionDefinition action) =>
            FreeTimeRules.Left(world, content, character) >= action.Afternoons;

        /// <summary>
        /// La hace: gasta sus tardes y aplica lo que cambia. Devuelve lo que cambió de verdad (una
        /// barra llena no sube más), en el orden de "cambia".
        /// </summary>
        /// <exception cref="InvalidOperationException">Si ahora no la puede hacer (no está disponible o no le alcanzan las tardes).</exception>
        public static IReadOnlyList<ActionChange> Do(World world, ContentDatabase content, Character character, string actionId, int? personId)
        {
            ActionOption option = Available(world, content, character, null)
                .FirstOrDefault(o => o.Definition.Id == actionId && o.Person?.Id == personId)
                ?? throw new InvalidOperationException($"Ahora no se puede hacer '{actionId}'" + (personId.HasValue ? $" con #{personId}." : "."));
            ActionDefinition action = option.Definition;
            if (!CanAfford(world, content, character, action))
            {
                throw new InvalidOperationException($"No quedan tardes libres este mes para '{actionId}'.");
            }

            FreeTimeRules.Spend(world, character, action.Afternoons, DoneKey(action, option.Person));
            var changes = new List<ActionChange>();
            foreach (KeyValuePair<string, int> pair in action.Effects)
            {
                int delta = Apply(world, character, option.Person, pair.Key, pair.Value);
                if (delta != 0)
                {
                    changes.Add(new ActionChange(pair.Key, delta));
                }
            }

            return changes;
        }

        private static int Apply(World world, Character character, Character? person, string key, int amount)
        {
            Stats stats = character.Stats;
            switch (key)
            {
                case "felicidad":
                    return Move(stats.Happiness, amount, v => stats.Happiness = v, () => stats.Happiness);
                case "salud":
                    return Move(stats.Health, amount, v => stats.Health = v, () => stats.Health);
                case "inteligencia":
                    return Move(stats.Smarts, amount, v => stats.Smarts = v, () => stats.Smarts);
                case "apariencia":
                    return Move(stats.Looks, amount, v => stats.Looks = v, () => stats.Looks);
                case "energia":
                    return Move(stats.Energy, amount, v => stats.Energy = v, () => stats.Energy);
                case "carino":
                    {
                        // El tiempo juntos se siente de los dos lados.
                        Relationship theirs = RelationshipRules.Edit(world, person!, character);
                        int before = theirs.Affection;
                        theirs.Affection += amount;
                        RelationshipRules.Edit(world, character, person!).Affection += amount;
                        return theirs.Affection - before;
                    }
                case "preparar_prueba":
                    {
                        Education.SchoolRecord record = character.SchoolRecord!;
                        int before = record.TestPreparation;
                        record.TestPreparation = Math.Max(0, Math.Min(Education.GradeRules.MaxPreparation, before + amount));
                        return record.TestPreparation - before;
                    }
                default:
                    throw new InvalidOperationException($"'{key}' no es algo que cambie una acción.");
            }
        }

        private static int Move(int before, int amount, Action<int> set, Func<int> get)
        {
            set(before + amount);
            return get() - before;
        }

        private static string DoneKey(ActionDefinition action, Character? person) =>
            person == null ? action.Id : action.Id + "#" + person.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static int TimesThisMonth(FreeTime? month, string key) => month?.Done.Count(d => d == key) ?? 0;
    }
}

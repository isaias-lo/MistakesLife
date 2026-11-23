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
            string? country = character.Residence?.CountryId;
            foreach (ActionDefinition action in content.Actions)
            {
                if ((group != null && action.Group != group) || age < action.MinAge || age > action.MaxAge
                    || (action.CountryId != null && action.CountryId != country)
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

                if (action.Alone && TimesThisMonth(month, DoneKey(action, null)) < action.PerMonth)
                {
                    // Se puede ir sin nadie (ver una película, entrenar): va primero, sin persona.
                    options.Add(new ActionOption(action, null));
                }

                foreach (Character person in People(world, content, character, action.With).OrderBy(p => p.Id))
                {
                    if (TimesThisMonth(month, DoneKey(action, person)) < action.PerMonth
                        && (action.CompiledPersonConditions == null || action.CompiledPersonConditions.Evaluate(PersonScope(world, content, character, person, readOnly: true)).AsBool(action.Id)))
                    {
                        options.Add(new ActionOption(action, person));
                    }
                }
            }

            return options;
        }

        /// <summary>
        /// Si le quedan tardes libres para hacerla este mes y, si cuesta plata y ya paga su parte
        /// (<see cref="ActionDefinition.PaysFromAge"/>), si le alcanza.
        /// </summary>
        public static bool CanAfford(World world, ContentDatabase content, Character character, ActionDefinition action) =>
            FreeTimeRules.Left(world, content, character) >= action.Afternoons
            && (Price(world, character, action) is decimal price && (price == 0 || character.Money >= price));

        /// <summary>
        /// Lo que paga el jugador por su parte, a precios de hoy (como <c>precio()</c>): 0 si no
        /// cuesta, si no hay precio para su país o si todavía no paga él (antes de los 18 paga su casa).
        /// </summary>
        public static decimal Price(World world, Character character, ActionDefinition action) =>
            character.AgeInYears(world.Date) >= ActionDefinition.PaysFromAge ? ListPrice(world, character, action) : 0m;

        /// <summary>Lo que cuesta la parte de una persona en su país, a precios de hoy, sin mirar quién paga; 0 si no cuesta.</summary>
        public static decimal ListPrice(World world, Character character, ActionDefinition action)
        {
            string? country = character.Residence?.CountryId;
            if (country == null || !action.Price.TryGetValue(country, out int amount))
            {
                return 0m;
            }

            double index = world.EconomyOf(country)?.PriceIndex ?? 1;
            return (decimal)Math.Round(amount * index, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// La hace: gasta sus tardes y, si la persona dice que sí (o la acción no lo pregunta),
        /// aplica lo que cambia y lo demás que pasa ("efectos"); si dice que no, solo lo de
        /// "si_no_acepta". Devuelve si dijo que sí y lo que cambió de verdad (una barra llena no
        /// sube más), en el orden de "cambia".
        /// </summary>
        /// <exception cref="InvalidOperationException">Si ahora no la puede hacer (no está disponible o no le alcanzan las tardes).</exception>
        public static ActionOutcome Do(World world, ContentDatabase content, Character character, string actionId, int? personId)
        {
            ActionOption option = Available(world, content, character, null)
                .FirstOrDefault(o => o.Definition.Id == actionId && o.Person?.Id == personId)
                ?? throw new InvalidOperationException($"Ahora no se puede hacer '{actionId}'" + (personId.HasValue ? $" con #{personId}." : "."));
            ActionDefinition action = option.Definition;
            if (!CanAfford(world, content, character, action))
            {
                throw new InvalidOperationException($"No quedan tardes libres este mes (o no alcanza la plata) para '{actionId}'.");
            }

            bool accepted = option.Person == null || action.CompiledAccepts == null
                || action.CompiledAccepts.Evaluate(PersonScope(world, content, character, option.Person, readOnly: true)).AsBool(action.Id);
            FreeTimeRules.Spend(world, character, action.Afternoons, DoneKey(action, option.Person));
            if (accepted)
            {
                // Se paga solo si fueron (si te dijo que no, no hubo cine).
                character.Move(world.Date, action.Id, -Price(world, character, action));
                if (option.Person != null)
                {
                    int week = world.Date.TotalWeeks;
                    RelationshipRules.Edit(world, character, option.Person).LastTogetherWeek = week;
                    RelationshipRules.Edit(world, option.Person, character).LastTogetherWeek = week;
                }
            }

            var changes = new List<ActionChange>();
            foreach (KeyValuePair<string, int> pair in accepted ? action.Effects : action.IfDeclined)
            {
                int delta = Apply(world, character, option.Person, pair.Key, pair.Value);
                if (delta != 0)
                {
                    changes.Add(new ActionChange(pair.Key, delta));
                }
            }

            if (action.With == ActionDefinition.Interesting)
            {
                // La gente interesante (D4b2): si te pescó, ya se conocen; si no, la puerta se va cerrando.
                if (accepted)
                {
                    InterestingPeopleRules.Meet(world, character, option.Person!);
                }
                else
                {
                    InterestingPeopleRules.Decline(world, character, option.Person!);
                }
            }

            if (accepted && action.CompiledScripts.Count > 0)
            {
                var scope = PersonScope(world, content, character, option.Person, readOnly: false);
                foreach (Scripting.Effect effect in action.CompiledScripts)
                {
                    effect.Apply(scope);
                }
            }

            return new ActionOutcome(accepted, changes);
        }

        /// <summary>
        /// Con quiénes se puede hacer una acción que dice "con": quienes ocupan ese lugar en su
        /// vida (como en Mensajito), sus conocidos o cualquiera con quien tenga relación
        /// (ver <see cref="ActionDefinition.ExtraRoles"/>).
        /// </summary>
        public static IReadOnlyList<Character> People(World world, ContentDatabase content, Character character, string role)
        {
            if (role == ActionDefinition.Interesting)
            {
                return InterestingPeopleRules.Shown(world, content, character);
            }

            if (role != ActionDefinition.Acquaintance && role != ActionDefinition.Anyone)
            {
                return ChatSystem.PeopleAs(world, character, role, 0);
            }

            var people = new List<Character>();
            foreach (Relationship relationship in character.Relationships)
            {
                if (world.TryGetCharacter(relationship.OtherId, out Character? other) && other.IsAlive && other.Id != character.Id
                    && (role == ActionDefinition.Anyone || IsAcquaintance(world, character, other, relationship)))
                {
                    people.Add(other);
                }
            }

            if (role == ActionDefinition.Anyone)
            {
                // Y la familia y la pareja, aunque todavía no tengan nada guardado entre ellos.
                var family = new List<Character>();
                foreach (string kin in new[] { "madre", "padre", "pareja", "hijo" })
                {
                    family.AddRange(ChatSystem.PeopleAs(world, character, kin, 0));
                }

                family.AddRange(world.Characters.Where(c => c.IsAlive && c.Id != character.Id && RelationshipRules.IsFamily(world, character, c)));
                foreach (Character parent in ChatSystem.PeopleAs(world, character, "madre", 0).Concat(ChatSystem.PeopleAs(world, character, "padre", 0)))
                {
                    foreach (int? id in new[] { parent.MotherId, parent.FatherId, parent.SecondParentId })
                    {
                        if (id.HasValue && world.TryGetCharacter(id.Value, out Character? grandparent) && grandparent.IsAlive)
                        {
                            family.Add(grandparent);
                        }
                    }
                }

                people.AddRange(family.Where(f => people.All(p => p.Id != f.Id)).GroupBy(f => f.Id).Select(g => g.First()));
            }

            return people;
        }

        /// <summary>
        /// Si es un conocido: alguien con quien tiene relación sin ser su familia (papás, hijos,
        /// hermanos, abuelos, nietos), su pareja, su amistad, su rival ni su ex. Un colega sí es conocido.
        /// </summary>
        public static bool IsAcquaintance(World world, Character character, Character other, Relationship relationship)
        {
            if (relationship.Bond != RelationshipBond.None && relationship.Bond != RelationshipBond.Colleague)
            {
                return false;
            }

            if (character.PartnerId == other.Id || RelationshipRules.IsFamily(world, character, other))
            {
                return false;
            }

            foreach (int? parentId in new[] { character.MotherId, character.FatherId, character.SecondParentId })
            {
                if (parentId.HasValue && world.TryGetCharacter(parentId.Value, out Character? parent) && parent.IsChildOf(other.Id))
                {
                    return false;
                }
            }

            // Ni sus nietos.
            foreach (int? parentId in new[] { other.MotherId, other.FatherId, other.SecondParentId })
            {
                if (parentId.HasValue && world.TryGetCharacter(parentId.Value, out Character? parent) && parent.IsChildOf(character.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private static EventScope PersonScope(World world, ContentDatabase content, Character character, Character? person, bool readOnly) =>
            new EventScope(world, content, character, null, readOnly, person);

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
                case "carino" when person == null:
                    // Fue sin nadie (ver una película solo): no hay cariño que cambie.
                    return 0;
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

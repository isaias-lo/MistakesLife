using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Life;
using MistakesLife.Core.Time;
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
                // Con "con": "testigo", el evento pasa con quien vio lo que hiciste: su nombre
                // sale en la escena y en Tu gente, en vez de "un amigo". Como cualquier evento,
                // respeta su "unico", sus "veces_maximas" y su enfriamiento.
                Character? witness = OldestLivingWitness(world, seed);
                var scope = new EventScope(world, Content, player, seed, readOnly: true);
                var witnessScope = new EventScope(world, Content, player, seed, readOnly: true, witness);
                List<EventDefinition> candidates = Content.Events
                    .Where(e => e.SeedType == seed.TypeId && CanRepeat(world, e, player))
                    .Where(e => e.With != EventDefinition.WitnessRole
                        ? Passes(e, scope)
                        : witness != null && Passes(e, witnessScope)
                            && !(e.PerPerson && e.MaxTimes.HasValue && world.TimesHappened(e.Id, player.Id, witness.Id) >= e.MaxTimes.Value))
                    .ToList();
                if (candidates.Count > 0)
                {
                    EventDefinition picked = PickWeighted(candidates, random);
                    Occur(world, context, picked, player, seed, picked.With == EventDefinition.WitnessRole ? witness : null);
                    return;
                }
            }

            // Momentos con fecha (un cumpleaños, Navidad, el primer día de clases) y
            // momentos que nacen de tu horario (la plaza con tu amigo, la once con tu mamá).
            // Su propio azar, para no cambiar el de los demás eventos.
            if (TryScheduledMoment(world, context, player, context.RandomFor(Id + ".momentos")))
            {
                return;
            }

            // Eventos con probabilidad anual: cada uno tiene su propia oportunidad cada semana.
            foreach (EventDefinition ev in Content.Events.Where(e => e.AnnualProbability.HasValue && !e.IsScheduled))
            {
                bool happens = ev.With == null
                    ? CanHappen(world, ev, player, new EventScope(world, Content, player, null, readOnly: true)) && random.Chance(ev.WeeklyProbability)
                    : CanRepeat(world, ev, player) && random.Chance(ev.WeeklyProbability) && TryOccur(world, context, ev, player, random);
                if (happens)
                {
                    if (ev.With == null)
                    {
                        Occur(world, context, ev, player, null, null);
                    }

                    return;
                }
            }

            if (!random.Chance(Rules.WeeklyRandomEventChance))
            {
                return;
            }

            var randomScope = new EventScope(world, Content, player, null, readOnly: true);
            List<EventDefinition> pool = Content.Events
                .Where(e => e.SeedType == null && !e.AnnualProbability.HasValue && !e.IsScheduled
                    && (e.With == null ? CanHappen(world, e, player, randomScope) : CanRepeat(world, e, player)))
                .ToList();
            while (pool.Count > 0)
            {
                EventDefinition picked = PickWeighted(pool, random);
                if (TryOccur(world, context, picked, player, random))
                {
                    return;
                }

                pool.Remove(picked);
            }
        }

        /// <summary>
        /// Los momentos que el jugador puede pedir ahora (los "a_pedido" cuyas condiciones cumple,
        /// con alguien que los cumpla si piden persona, y que no estén en su enfriamiento).
        /// </summary>
        public IReadOnlyList<EventDefinition> Requestable(World world)
        {
            Character player = world.Player;
            if (!player.IsAlive || world.PendingEvents.Any(p => p.CharacterId == player.Id))
            {
                return Array.Empty<EventDefinition>();
            }

            return Content.Events
                .Where(e => e.OnRequest != null && CanRequest(world, e, player))
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// El jugador pide un momento (un botón): si se puede, pasa ahora mismo y queda
        /// pendiente para que elija. Devuelve si pasó.
        /// </summary>
        public bool Request(World world, string eventId)
        {
            EventDefinition? ev = Content.Events.FirstOrDefault(e => e.Id == eventId && e.OnRequest != null);
            Character player = world.Player;
            if (ev == null || !player.IsAlive || world.PendingEvents.Any(p => p.CharacterId == player.Id) || !CanRequest(world, ev, player))
            {
                return false;
            }

            GameRandom random = world.Random.Fork("pedido." + ev.Id + "." + world.Date.TotalWeeks);
            return TryOccur(world, null, ev, player, random);
        }

        /// <summary>
        /// Los momentos que el jugador puede pedir ahora con esa persona (los "a_pedido" con
        /// `con` en los que esa persona calza), para los botones de su ficha.
        /// </summary>
        public IReadOnlyList<EventDefinition> RequestableWith(World world, int personId)
        {
            Character player = world.Player;
            if (!player.IsAlive || world.PendingEvents.Any(p => p.CharacterId == player.Id)
                || !world.TryGetCharacter(personId, out Character? person) || !person.IsAlive)
            {
                return Array.Empty<EventDefinition>();
            }

            return Content.Events
                .Where(e => e.OnRequest != null && e.With != null && CanRequestWith(world, e, player, person))
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>El jugador pide un momento con esa persona (un botón de su ficha). Devuelve si pasó.</summary>
        public bool Request(World world, string eventId, int personId)
        {
            EventDefinition? ev = Content.Events.FirstOrDefault(e => e.Id == eventId && e.OnRequest != null && e.With != null);
            Character player = world.Player;
            if (ev == null || !player.IsAlive || world.PendingEvents.Any(p => p.CharacterId == player.Id)
                || !world.TryGetCharacter(personId, out Character? person) || !CanRequestWith(world, ev, player, person))
            {
                return false;
            }

            Occur(world, null, ev, player, null, person);
            return true;
        }

        private bool CanRequestWith(World world, EventDefinition ev, Character player, Character person) =>
            CanRepeat(world, ev, player)
            && PeopleFor(world, player, ev.With!).Any(p => p.Id == person.Id)
            && Passes(ev, new EventScope(world, Content, player, null, readOnly: true, person));

        private bool CanRequest(World world, EventDefinition ev, Character player)
        {
            if (ev.With == null)
            {
                return CanHappen(world, ev, player, new EventScope(world, Content, player, null, readOnly: true));
            }

            return CanRepeat(world, ev, player)
                && PeopleFor(world, player, ev.With)
                    .Any(p => Passes(ev, new EventScope(world, Content, player, null, readOnly: true, p)));
        }

        /// <summary>
        /// Prueba los momentos con fecha que caen esta semana y los que nacen del horario.
        /// Devuelve true si ocurrió uno.
        /// </summary>
        private bool TryScheduledMoment(World world, WeekContext context, Character player, GameRandom random)
        {
            foreach (EventDefinition ev in Content.Events.Where(e => e.Date != null))
            {
                if (!IsDateThisWeek(world, ev) || HappenedThisYear(world, ev, player))
                {
                    continue;
                }

                if (ev.AnnualProbability.HasValue && !random.Chance(ev.AnnualProbability.Value))
                {
                    continue;
                }

                if (TryOccur(world, context, ev, player, random))
                {
                    return true;
                }
            }

            Routine? routine = player.Routine;
            if (routine == null)
            {
                return false;
            }

            foreach (EventDefinition ev in Content.Events.Where(e => e.Activity != null))
            {
                int times = routine.Count(ev.Activity!);
                if (times == 0)
                {
                    continue;
                }

                // Hacerla varias veces por semana: cada vez es una oportunidad.
                double chance = 1.0 - Math.Pow(1.0 - ev.WeeklyProbability, times);
                if (random.Chance(chance) && TryOccur(world, context, ev, player, random))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDateThisWeek(World world, EventDefinition ev) =>
            ev.FixedDayOfYear.HasValue && world.Date.ContainsDayOfYear(ev.FixedDayOfYear.Value) || ev.OnBirthday;

        private static bool HappenedThisYear(World world, EventDefinition ev, Character player) =>
            world.EventHistory.Any(r => r.EventId == ev.Id && r.CharacterId == player.Id && world.Date.WeeksSince(r.Date) < GameDate.WeeksPerYear);

        /// <summary>El testigo vivo más antiguo de la semilla (el primero que vio lo que pasó), o nadie.</summary>
        private static Character? OldestLivingWitness(World world, Seed seed)
        {
            foreach (int witnessId in seed.Witnesses.OrderBy(w => w))
            {
                if (world.TryGetCharacter(witnessId, out Character? witness) && witness.IsAlive)
                {
                    return witness;
                }
            }

            return null;
        }

        /// <summary>
        /// Ocurre el evento si se cumplen sus condiciones. Si pasa con alguien, primero se
        /// elige a esa persona (en un cumpleaños, alguien que esté de cumpleaños esta semana).
        /// </summary>
        private bool TryOccur(World world, WeekContext? context, EventDefinition ev, Character player, GameRandom random)
        {
            if (ev.With == null)
            {
                var scope = new EventScope(world, Content, player, null, readOnly: true);
                if (!CanHappen(world, ev, player, scope))
                {
                    return false;
                }

                Occur(world, context, ev, player, null, null);
                return true;
            }

            if (!CanRepeat(world, ev, player))
            {
                return false;
            }

            if (EventDefinition.IsNewPersonRole(ev.With))
            {
                // Conoces a alguien nuevo: primero se revisa si puede pasar y después nace la persona.
                Character? partner = player.PartnerId.HasValue && world.TryGetCharacter(player.PartnerId.Value, out Character? found) && found.IsAlive ? found : null;
                bool ofPartner = ev.With == EventDefinition.NewPersonOfPartnerRole;
                if ((ofPartner && (partner == null || partner.Residence == null)) || !Passes(ev, new EventScope(world, Content, player, null, readOnly: true)))
                {
                    return false;
                }

                Character stranger = FamilyFactory.CreateStranger(world, Content, ofPartner ? partner! : player);
                if (ofPartner)
                {
                    // Tú también sabes quién es: queda en tu gente.
                    RelationshipRules.SetBond(world, player, stranger, RelationshipBond.None);
                }

                Occur(world, context, ev, player, null, stranger);
                return true;
            }

            List<Character> people = PeopleFor(world, player, ev.With)
                .Where(p => !ev.OnBirthday || world.Date.SameWeekOfYear(p.BirthDate))
                .Where(p => !(ev.PerPerson && ev.MaxTimes.HasValue && world.TimesHappened(ev.Id, player.Id, p.Id) >= ev.MaxTimes.Value))
                .ToList();
            while (people.Count > 0)
            {
                Character person = ev.With == "companero" || ev.With == "equipo" ? CloserClassmate(world, player, people, random) : people[random.NextInt(people.Count)];
                var scope = new EventScope(world, Content, player, null, readOnly: true, person);
                if (Passes(ev, scope))
                {
                    Occur(world, context, ev, player, null, person);
                    return true;
                }

                people.Remove(person);
            }

            return false;
        }

        /// <summary>
        /// En un curso no pasas el mismo tiempo con todos: un momento con un compañero cae
        /// más seguido con los que se quieren más (el peso crece con el cuadrado del cariño; cuenta
        /// el mayor de los dos, porque quien te quiere también te busca).
        /// Así se arma tu grupo y recuerdas sus nombres.
        /// </summary>
        private static Character CloserClassmate(World world, Character player, IReadOnlyList<Character> people, GameRandom random)
        {
            var weights = people.Select(p =>
            {
                int affection = Math.Max(RelationshipRules.Feelings(world, player, p).Affection, RelationshipRules.Feelings(world, p, player).Affection);
                return 1 + (affection * affection / 100);
            }).ToList();
            return people[random.PickWeightedIndex(weights)];
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
            world.RemovePendingEvent(pending);
            Character? person = PersonOf(world, pending);
            var condition = definition.Options[optionIndex].CompiledConditions;
            if (condition != null && !condition.IsTrue(new EventScope(world, Content, subject, seed, readOnly: true, person)))
            {
                // Entre que apareció el momento y se eligió, algo de la misma semana cambió (otro
                // sistema, otra decisión) y la opción ya no se puede: se anota sin sus efectos, en
                // vez de aplicar algo que ya no corresponde.
                Finish(world, definition, subject, seed, optionIndex, person?.Id);
                return;
            }

            ApplyOption(world, definition, subject, seed, person, optionIndex);
        }

        private void ApplyOption(World world, EventDefinition definition, Character subject, Seed? seed, Character? person, int optionIndex)
        {
            var scope = new EventScope(world, Content, subject, seed, readOnly: false, person)
            {
                Origin = new SeedOrigin(definition.Id, definition.Options[optionIndex].TextKey, definition.Place, person?.Id),
            };
            foreach (var effect in definition.Options[optionIndex].CompiledEffects)
            {
                effect.Apply(scope);
            }

            Finish(world, definition, subject, seed, optionIndex, person?.Id);
        }

        /// <summary>
        /// La regla propia que decide este evento, si hay una y su opción está disponible:
        /// devuelve la opción (índice en el evento) o null si hay que preguntar.
        /// </summary>
        public static int? RuleChoice(Character subject, EventDefinition definition, IReadOnlyList<int> available, out OwnRule? rule)
        {
            rule = null;
            if (!definition.AllowsOwnRule)
            {
                return null;
            }

            rule = subject.OwnRules.FirstOrDefault(r => r.Key == definition.RuleKey);
            if (rule == null)
            {
                return null;
            }

            foreach (int option in available)
            {
                if (definition.Options[option].RuleKey == rule.Choice)
                {
                    return option;
                }
            }

            rule = null;
            return null;
        }

        /// <summary>Con quién pasa el momento pendiente; null si es sin nadie o esa persona ya no está.</summary>
        public static Character? PersonOf(World world, PendingEvent pending) =>
            pending.PersonId.HasValue && world.TryGetCharacter(pending.PersonId.Value, out Character? person) ? person : null;

        private void Occur(World world, WeekContext? context, EventDefinition definition, Character subject, Seed? seed, Character? person)
        {
            var scope = new EventScope(world, Content, subject, seed, readOnly: false, person)
            {
                Origin = new SeedOrigin(definition.Id, null, definition.Place, person?.Id),
            };
            foreach (var effect in definition.CompiledEffects)
            {
                effect.Apply(scope);
            }

            var readScope = new EventScope(world, Content, subject, seed, readOnly: true, person);
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
                context?.RequestStop(definition.Id);
            }

            if (options.Count == 0)
            {
                Finish(world, definition, subject, seed, null, person?.Id);
                return;
            }

            // Lo ya decidido no se vuelve a preguntar: si el jugador tiene una regla, se elige sola.
            int? ruled = RuleChoice(subject, definition, options, out OwnRule? rule);
            if (ruled.HasValue && rule != null)
            {
                rule.TimesUsed++;
                rule.LastUsed = world.Date;
                ApplyOption(world, definition, subject, seed, person, ruled.Value);
                return;
            }

            world.AddPendingEvent(new PendingEvent(definition.Id, subject.Id, world.Date, seed?.Id, options, person?.Id));
        }

        private static void Finish(World world, EventDefinition definition, Character subject, Seed? seed, int? option, int? personId = null)
        {
            world.RecordEvent(new EventRecord(world.Date, definition.Id, subject.Id, option, personId));
            if (seed != null && seed.IsOpen)
            {
                world.Seeds.Resolve(seed, world.Date);
            }
        }

        /// <summary>
        /// Quiénes calzan con el "con" de un momento: como en Mensajito, o los conocidos y cualquiera
        /// con quien tenga relación (como en las acciones, ver Life.ActionRules.People).
        /// Alguien nuevo todavía no existe: nadie.
        /// </summary>
        private IReadOnlyList<Character> PeopleFor(World world, Character player, string role)
        {
            if (EventDefinition.IsNewPersonRole(role))
            {
                return Array.Empty<Character>();
            }

            if (role == EventDefinition.DeceasedRole)
            {
                return GriefRules.Departed(world, player);
            }

            return Array.IndexOf(ActionDefinition.ExtraRoles, role) >= 0
                ? ActionRules.People(world, Content, player, role)
                : ChatSystem.PeopleAs(world, player, role, minAge: 0);
        }

        private static bool CanHappen(World world, EventDefinition definition, Character player, EventScope scope) =>
            CanRepeat(world, definition, player) && Passes(definition, scope);

        /// <summary>Si el evento todavía puede pasar: no es único ya vivido, no llegó a su tope ni está en espera.</summary>
        private static bool CanRepeat(World world, EventDefinition definition, Character player) =>
            !(definition.Unique && HasHappened(world, definition.Id, player.Id))
            && !(definition.MaxTimes.HasValue && !definition.PerPerson && world.TimesHappened(definition.Id, player.Id) >= definition.MaxTimes.Value)
            && !InCooldown(world, definition, player.Id);

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

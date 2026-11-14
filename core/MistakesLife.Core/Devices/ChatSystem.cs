using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// Fase de sistemas, al empezar la semana (antes de los eventos, para que un evento vea lo
    /// que cambió al dejar a alguien en visto): Mensajito. A quien tiene la app,
    /// le escriben las personas reales de su vida (su mamá, su pareja, sus amigos, un
    /// ex) según packs/*/chats/:
    /// <list type="bullet">
    /// <item>por algo que pasó: el mensaje escucha una línea del diario del jugador
    /// ("al_anotar"), como su cumpleaños, que nació su hijo o que terminó con su pareja. El
    /// mensaje lleva la fecha de esa línea: lo anotado al final de una semana se lee al empezar
    /// la siguiente, pero el saludo de cumpleaños es del día del cumpleaños;</item>
    /// <item>porque se acordaron de él: cada semana, con su "probabilidad", si no están
    /// esperando que les contesten y no le mandaron lo mismo en el último medio año.</item>
    /// </list>
    /// Solo escribe quien siente lo que pide el mensaje (un amigo que ya no lo quiere no
    /// lo saluda para su cumpleaños). Si el jugador vio el mensaje y no contesta en
    /// <see cref="ReplyWeeks"/> semanas, lo dejó en visto y la otra persona lo siente
    /// ("si_no_respondes"); si ni lo abrió, espera <see cref="UnseenWeeks"/> semanas.
    /// </summary>
    public sealed class ChatSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.mensajito";

        /// <summary>La app que hay que tener en el celular.</summary>
        public const string AppId = "app.mensajito";

        /// <summary>Semanas que alguien espera una respuesta, si el jugador ya vio el mensaje, antes de sentirse dejado en visto.</summary>
        public const int ReplyWeeks = 2;

        /// <summary>Semanas que espera si el jugador ni siquiera abrió la conversación: después, también se siente ignorado.</summary>
        public const int UnseenWeeks = 6;

        /// <summary>Edad desde la que una persona escribe por Mensajito (un hijo, un amigo del colegio).</summary>
        public const int MinSenderAge = 12;

        /// <summary>Cuántas personas, como mucho, escriben por una misma línea del diario (las que más lo quieren).</summary>
        public const int MaxSendersPerLine = 5;

        /// <summary>Semanas en que una persona no repite el mismo mensaje "de vez en cuando".</summary>
        public const int RepeatWeeks = 26;

        private readonly ContentDatabase _content;

        public ChatSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            GameRandom random = context.RandomFor(SystemId);
            foreach (Character character in world.Characters.Where(c => c.IsAlive).OrderBy(c => c.Id))
            {
                if (character.Phone == null || !character.Phone.Has(AppId))
                {
                    if (character.Chats != null)
                    {
                        // Sin la app no llega nada; al volver a tenerla, no llega lo de antes.
                        character.Chats.JournalCursor = world.Journal.Count;
                    }

                    continue;
                }

                character.Chats ??= new ChatInbox(world.Journal.Count);
                ExpireUnanswered(world, _content, character);
                DeliverForJournal(world, _content, character);
                DeliverRandom(world, _content, character, random);
            }
        }

        /// <summary>
        /// El jugador contesta el mensaje que espera respuesta en su conversación con
        /// <paramref name="otherId"/>, con la respuesta número <paramref name="replyIndex"/>
        /// del mensaje. Lo que contesta cambia lo que la otra persona siente.
        /// </summary>
        public static void Reply(World world, ContentDatabase content, Character character, int otherId, int replyIndex)
        {
            Chat chat = character.Chats?.With(otherId) ?? throw new InvalidOperationException($"No hay conversación con #{otherId}.");
            ChatDefinition definition = (chat.AwaitingReply == null ? null : content.Chat(chat.AwaitingReply))
                ?? throw new InvalidOperationException($"La conversación con #{otherId} no espera respuesta.");
            if (replyIndex < 0 || replyIndex >= definition.Replies.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(replyIndex), $"El mensaje {definition.Id} no tiene respuesta número {replyIndex}.");
            }

            ChatReplyDefinition reply = definition.Replies[replyIndex];
            chat.Add(new ChatMessage(world.Date, reply.Id, fromPlayer: true));
            chat.AwaitingReply = null;
            chat.Unread = 0;
            if (world.TryGetCharacter(otherId, out Character? other) && other.IsAlive)
            {
                Apply(world, other, character, reply);
            }
        }

        /// <summary>
        /// Las personas que ocupan ese lugar en la vida de <paramref name="character"/>
        /// (ver <see cref="ChatDefinition.Senders"/>), vivas y con edad para escribir.
        /// </summary>
        public static IReadOnlyList<Character> PeopleAs(World world, Character character, string role)
        {
            var ids = new List<int>();
            switch (role)
            {
                case "madre":
                    AddParents(world, character, ids, female: true);
                    break;
                case "padre":
                    AddParents(world, character, ids, female: false);
                    break;
                case "pareja":
                    if (character.PartnerId.HasValue)
                    {
                        ids.Add(character.PartnerId.Value);
                    }

                    break;
                case "hijo":
                    ids.AddRange(world.ChildrenOf(character.Id).Select(c => c.Id));
                    break;
                case "amigo":
                    ids.AddRange(RelationshipRules.WithBond(world, character, RelationshipBond.Friend).Select(c => c.Id));
                    break;
                case "colega":
                    ids.AddRange(RelationshipRules.WithBond(world, character, RelationshipBond.Colleague).Select(c => c.Id));
                    break;
                case "rival":
                    ids.AddRange(RelationshipRules.WithBond(world, character, RelationshipBond.Rival).Select(c => c.Id));
                    break;
                case "ex":
                    ids.AddRange(RelationshipRules.WithBond(world, character, RelationshipBond.Ex).Select(c => c.Id));
                    break;
                default:
                    throw new ArgumentException($"'{role}' no es alguien que escribe ({string.Join(", ", ChatDefinition.Senders)}).", nameof(role));
            }

            var people = new List<Character>();
            foreach (int id in ids.Distinct())
            {
                if (id != character.Id && world.TryGetCharacter(id, out Character? person) && person.IsAlive && person.AgeInYears(world.Date) >= MinSenderAge)
                {
                    people.Add(person);
                }
            }

            return people;
        }

        private static void AddParents(World world, Character character, List<int> ids, bool female)
        {
            if (female && character.MotherId.HasValue)
            {
                ids.Add(character.MotherId.Value);
            }

            if (!female && character.FatherId.HasValue)
            {
                ids.Add(character.FatherId.Value);
            }

            // Si tiene dos mamás o dos papás, el segundo escribe como lo que es.
            if (character.SecondParentId.HasValue && world.TryGetCharacter(character.SecondParentId.Value, out Character? second)
                && (second.Sex == Sex.Female) == female)
            {
                ids.Add(second.Id);
            }
        }

        /// <summary>Quienes escribirían este mensaje ahora: los que ocupan su lugar y sienten lo que pide, los que más lo quieren primero.</summary>
        private static IReadOnlyList<Character> Writers(World world, ContentDatabase content, Character character, ChatDefinition definition)
        {
            if (definition.CompiledConditions != null
                && !definition.CompiledConditions.Evaluate(new EventScope(world, content, character, null, readOnly: true)).AsBool(definition.Id))
            {
                return Array.Empty<Character>();
            }

            return PeopleAs(world, character, definition.From)
                .Select(p => (Person: p, Feelings: RelationshipRules.Feelings(world, p, character)))
                .Where(x => x.Person.AgeInYears(world.Date) >= definition.MinAge && x.Person.AgeInYears(world.Date) <= definition.MaxAge)
                .Where(x => x.Feelings.Affection >= definition.MinAffection && x.Feelings.Affection <= definition.MaxAffection
                    && x.Feelings.Resentment >= definition.MinResentment && x.Feelings.Resentment <= definition.MaxResentment)
                .OrderByDescending(x => x.Feelings.Affection)
                .ThenBy(x => x.Person.Id)
                .Select(x => x.Person)
                .ToList();
        }

        private static void Send(Time.GameDate date, Character character, Character writer, ChatDefinition definition) =>
            character.Chats!.Receive(writer.Id, date, definition.Id, expectsReply: definition.Replies.Count > 0);

        /// <summary>Los mensajes que nadie contestó a tiempo: la otra persona se siente dejada en visto.</summary>
        private static void ExpireUnanswered(World world, ContentDatabase content, Character character)
        {
            foreach (Chat chat in character.Chats!.Chats)
            {
                int patience = chat.Unread > 0 ? UnseenWeeks : ReplyWeeks;
                if (chat.AwaitingReply == null || world.Date.WeeksSince(chat.AwaitingSince) < patience)
                {
                    continue;
                }

                ChatDefinition? definition = content.Chat(chat.AwaitingReply);
                chat.AwaitingReply = null;
                if (definition?.IfIgnored != null && world.TryGetCharacter(chat.OtherId, out Character? other) && other.IsAlive)
                {
                    Apply(world, other, character, definition.IfIgnored);
                }
            }
        }

        /// <summary>Los mensajes por lo que el jugador anotó en su diario desde la última vez.</summary>
        private static void DeliverForJournal(World world, ContentDatabase content, Character character)
        {
            ChatInbox inbox = character.Chats!;
            IReadOnlyList<JournalEntry> journal = world.Journal;
            for (int i = Math.Min(inbox.JournalCursor, journal.Count); i < journal.Count; i++)
            {
                JournalEntry entry = journal[i];
                if (entry.CharacterId != character.Id)
                {
                    continue;
                }

                foreach (ChatDefinition definition in content.ChatsForJournal(entry.TextKey))
                {
                    foreach (Character writer in Writers(world, content, character, definition).Take(MaxSendersPerLine))
                    {
                        Send(entry.Date, character, writer, definition);
                    }
                }
            }

            inbox.JournalCursor = journal.Count;
        }

        /// <summary>
        /// Los que se acordaron del jugador esta semana: cada persona escribe como mucho
        /// un mensaje "de vez en cuando" por semana, y no si espera que le contesten.
        /// </summary>
        private static void DeliverRandom(World world, ContentDatabase content, Character character, GameRandom random)
        {
            var candidates = new Dictionary<int, List<ChatDefinition>>();
            var writers = new Dictionary<int, Character>();
            foreach (ChatDefinition definition in content.RandomChats)
            {
                foreach (Character writer in Writers(world, content, character, definition))
                {
                    Chat? chat = character.Chats!.With(writer.Id);
                    if (chat?.AwaitingReply != null || SentRecently(world, chat, definition.Id))
                    {
                        continue;
                    }

                    if (random.Chance(definition.Chance!.Value))
                    {
                        if (!candidates.TryGetValue(writer.Id, out List<ChatDefinition>? list))
                        {
                            list = new List<ChatDefinition>();
                            candidates.Add(writer.Id, list);
                            writers.Add(writer.Id, writer);
                        }

                        list.Add(definition);
                    }
                }
            }

            foreach (KeyValuePair<int, List<ChatDefinition>> pair in candidates.OrderBy(p => p.Key))
            {
                Send(world.Date, character, writers[pair.Key], random.Pick(pair.Value));
            }
        }

        private static bool SentRecently(World world, Chat? chat, string chatId) =>
            chat != null && chat.Messages.Any(m => !m.FromPlayer && m.TextKey == chatId && world.Date.WeeksSince(m.Date) < RepeatWeeks);

        /// <summary>Cambia lo que <paramref name="writer"/> siente por <paramref name="character"/>.</summary>
        private static void Apply(World world, Character writer, Character character, ChatFeelingsDefinition change)
        {
            if (change.Affection == 0 && change.Trust == 0 && change.Respect == 0 && change.Resentment == 0)
            {
                return;
            }

            Relationship feelings = RelationshipRules.Edit(world, writer, character);
            feelings.Affection += change.Affection;
            feelings.Trust += change.Trust;
            feelings.Respect += change.Respect;
            feelings.Resentment += change.Resentment;
        }
    }
}

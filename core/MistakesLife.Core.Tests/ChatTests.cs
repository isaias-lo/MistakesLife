using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class ChatTests
    {
        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        /// <summary>Una vida en Chile que ya tiene <paramref name="age"/> años, con celular y Mensajito.</summary>
        private static LifeSession Session(int age = 20, ulong seed = 5, bool app = true)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            Run(session, "jugador.dar_celular()");
            if (app)
            {
                Run(session, "jugador.instalar_app('app.mensajito')");
            }

            return session;
        }

        /// <summary>Corre solo Mensajito durante <paramref name="weeks"/> semanas.</summary>
        private static void ChatWeeks(LifeSession session, int weeks) =>
            new WeekCycle().Register(new ChatSystem(session.Content)).AdvanceWeeks(session.World, weeks);

        private static Character Player(LifeSession session) => session.World.Player;

        private static Character Mother(LifeSession session) => session.World.GetCharacter(Player(session).MotherId!.Value);

        private static IEnumerable<string> From(LifeSession session, int otherId) =>
            Player(session).Chats?.With(otherId)?.Messages.Where(m => !m.FromPlayer).Select(m => m.TextKey) ?? Enumerable.Empty<string>();

        private static Character NewFriend(LifeSession session)
        {
            Run(session, "jugador.conocer('amistad')");
            return RelationshipRules.WithBond(session.World, Player(session), RelationshipBond.Friend).OrderBy(c => c.Id).Last();
        }

        /// <summary>Lo que <paramref name="who"/> siente por el jugador.</summary>
        private static Relationship Feelings(LifeSession session, Character who) => RelationshipRules.Feelings(session.World, who, Player(session));

        [Fact]
        public void Cumpleanos_LeEscribenSuMamaYElAmigoQueLoQuiere_NoElQueSeEnfrio()
        {
            LifeSession session = Session();
            Character close = NewFriend(session);
            Character cold = NewFriend(session);
            RelationshipRules.Edit(session.World, cold, Player(session)).Affection = 20;
            ChatWeeks(session, 1);

            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);

            Assert.Contains("chat.madre.cumpleanos", From(session, Mother(session).Id));
            Assert.Contains("chat.amigo.cumpleanos", From(session, close.Id));
            Assert.DoesNotContain("chat.amigo.cumpleanos", From(session, cold.Id));
        }

        [Fact]
        public void SinMensajito_NadieEscribe_YNoLlegaLoDeAntes()
        {
            LifeSession session = Session(app: false);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);
            Assert.Null(Player(session).Chats);

            Run(session, "jugador.instalar_app('app.mensajito')");
            ChatWeeks(session, 1);

            Assert.NotNull(Player(session).Chats);
            Assert.DoesNotContain("chat.madre.cumpleanos", From(session, Mother(session).Id));
        }

        [Fact]
        public void MamaQueMurio_NoEscribe()
        {
            LifeSession session = Session();
            Mother(session).Die(session.World.Date);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);

            Assert.Empty(From(session, Mother(session).Id));
        }

        [Fact]
        public void Contestar_SubeElCarino_YQuedaEnLaConversacion()
        {
            LifeSession session = Session();
            Character mother = Mother(session);
            ChatWeeks(session, 1);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);
            int before = Feelings(session, mother).Affection;
            Chat chat = Player(session).Chats!.With(mother.Id)!;
            Assert.Equal("chat.madre.cumpleanos", chat.AwaitingReply);
            Assert.True(chat.Unread > 0);

            session.ReplyChat(mother.Id, 0);

            Assert.Equal(before + 2, Feelings(session, mother).Affection);
            Assert.Null(chat.AwaitingReply);
            Assert.Equal(0, chat.Unread);
            Assert.True(chat.Messages.Last().FromPlayer);
            Assert.Equal("chat.madre.cumpleanos.resp.te_quiero", chat.Messages.Last().TextKey);
            Assert.Throws<System.InvalidOperationException>(() => session.ReplyChat(mother.Id, 0));
        }

        [Fact]
        public void DejarEnVisto_DosSemanas_BajaElCarino()
        {
            LifeSession session = Session();
            Character mother = Mother(session);
            ChatWeeks(session, 1);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);
            int before = Feelings(session, mother).Affection;
            Chat chat = Player(session).Chats!.With(mother.Id)!;
            int sentBefore = chat.Messages.Count;
            session.ReadChat(mother.Id);

            ChatWeeks(session, ChatSystem.ReplyWeeks + 1);

            Assert.True(chat.AwaitingReply == null || chat.Messages.Count > sentBefore);
            Assert.True(Feelings(session, mother).Affection < before, $"el cariño pasó de {before} a {Feelings(session, mother).Affection}.");
        }

        [Fact]
        public void SinAbrirElChat_EsperaMas_PeroTambienSeSienteIgnorada()
        {
            LifeSession session = Session();
            Character mother = Mother(session);
            ChatWeeks(session, 1);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);
            int before = Feelings(session, mother).Affection;
            Chat chat = Player(session).Chats!.With(mother.Id)!;

            ChatWeeks(session, ChatSystem.ReplyWeeks + 1);
            Assert.Equal("chat.madre.cumpleanos", chat.AwaitingReply);

            ChatWeeks(session, ChatSystem.UnseenWeeks);
            Assert.NotEqual("chat.madre.cumpleanos", chat.AwaitingReply);
            Assert.True(Feelings(session, mother).Affection < before);
        }

        [Fact]
        public void DeVezEnCuando_NadieRepiteElMismoMensajeEnMedioAnio_NiEscribeSiEsperaRespuesta()
        {
            LifeSession session = Session(age: 22, seed: 11);
            NewFriend(session);
            NewFriend(session);
            Run(session, "jugador.formar_pareja()");
            ChatWeeks(session, 3 * GameDate.WeeksPerYear);

            ChatInbox inbox = Player(session).Chats!;
            Assert.True(inbox.Chats.Count >= 3, $"solo {inbox.Chats.Count} personas escribieron en 3 años.");
            foreach (Chat chat in inbox.Chats)
            {
                var incoming = chat.Messages.Where(m => !m.FromPlayer).ToList();
                for (int i = 1; i < incoming.Count; i++)
                {
                    ChatDefinition definition = session.Content.Chat(incoming[i].TextKey)!;
                    if (definition.Chance == null)
                    {
                        continue;
                    }

                    Assert.True(incoming[i].Date.WeeksSince(incoming[i - 1].Date) >= ChatSystem.ReplyWeeks || session.Content.Chat(incoming[i - 1].TextKey)!.Replies.Count == 0,
                        $"#{chat.OtherId} volvió a escribir {incoming[i].TextKey} sin esperar respuesta.");
                    var same = incoming.Take(i).Where(m => m.TextKey == incoming[i].TextKey).ToList();
                    Assert.True(same.Count == 0 || incoming[i].Date.WeeksSince(same.Last().Date) >= ChatSystem.RepeatWeeks,
                        $"#{chat.OtherId} repitió {incoming[i].TextKey} antes de medio año.");
                }
            }
        }

        [Fact]
        public void Guardar_ConservaLasConversaciones()
        {
            LifeSession session = Session();
            ChatWeeks(session, 1);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);
            session.ReplyChat(Mother(session).Id, 1);
            ChatWeeks(session, 20);

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            ChatInbox before = Player(session).Chats!;
            ChatInbox after = loaded.World.Player.Chats!;
            Assert.Equal(before.JournalCursor, after.JournalCursor);
            Assert.Equal(before.Chats.Select(c => (c.OtherId, c.Unread, c.AwaitingReply, c.AwaitingSince)), after.Chats.Select(c => (c.OtherId, c.Unread, c.AwaitingReply, c.AwaitingSince)));
            Assert.Equal(
                before.Chats.SelectMany(c => c.Messages.Select(m => (m.Date, m.TextKey, m.FromPlayer))),
                after.Chats.SelectMany(c => c.Messages.Select(m => (m.Date, m.TextKey, m.FromPlayer))));
        }

        [Fact]
        public void Pantalla_MuestraLaListaYLaConversacion_ConSusRespuestas()
        {
            LifeSession session = Session();
            var life = new Presentation.LifePresenter(session);
            Assert.Empty(life.Chats());
            ChatWeeks(session, 1);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);
            int mother = Mother(session).Id;

            Presentation.ChatSummaryView row = life.Chats().Single(c => c.OtherId == mother);
            Assert.Equal(session.Text("ui.relacion.madre"), row.Name);
            Assert.True(row.Unread > 0);
            Assert.True(row.AwaitingReply);
            Assert.True(life.Phone().UnreadChats > 0);
            Assert.True(life.Phone().Apps.Single(a => a.AppId == ChatSystem.AppId).Unread > 0);

            Presentation.ChatView chat = life.Chat(mother)!;
            Assert.Equal(session.Text("chat.madre.cumpleanos"), chat.Messages.Last().Text);
            Assert.Equal(2, chat.Replies.Count);
            life.ReadChat(mother);
            Assert.Equal(0, life.Chats().Single(c => c.OtherId == mother).Unread);

            life.ReplyChat(mother, 0);
            chat = life.Chat(mother)!;
            Assert.Empty(chat.Replies);
            Assert.True(chat.Messages.Last().FromPlayer);
            Assert.True(life.Chats().Single(c => c.OtherId == mother).LastFromPlayer);
        }

        [Fact]
        public void ConUnEventoEnPantalla_PrimeroSeDecide_DespuesSeContesta()
        {
            var session = new LifeSession(RepoPacks.Content, 1, "pais.chile");
            session.TimeMachine.Enabled = false;
            var life = new Presentation.LifePresenter(session);
            Chat? waiting = null;
            for (int step = 0; step < 4000 && life.Phase != Presentation.LifePhase.Over && session.PlayerAgeInYears < 40; step++)
            {
                waiting = session.World.Player.Chats?.Chats.FirstOrDefault(c => c.AwaitingReply != null);
                if (life.Phase == Presentation.LifePhase.Choosing && waiting != null)
                {
                    break;
                }

                if (life.Phase == Presentation.LifePhase.Living)
                {
                    life.AdvanceUntilSomething();
                }
                else if (life.Phase == Presentation.LifePhase.Choosing)
                {
                    life.Choose(0);
                }
                else
                {
                    break;
                }
            }

            Assert.Equal(Presentation.LifePhase.Choosing, life.Phase);
            Presentation.ChatView chat = life.Chat(waiting!.OtherId)!;
            Assert.Empty(chat.Replies);
            Assert.True(chat.ReplyAfterEvent);
            Assert.Throws<System.InvalidOperationException>(() => session.ReplyChat(waiting.OtherId, 0));
        }

        [Fact]
        public void MensajeDeAlguienInventadoOSinMotivo_SeDetecta()
        {
            var content = new ContentDatabase();
            content.AddChatsJson("pack.prueba", "chats.json", @"[
              { ""id"": ""chat.a"", ""de"": ""vecino"", ""probabilidad"": 0.1 },
              { ""id"": ""chat.b"", ""de"": ""madre"" },
              { ""id"": ""chat.c"", ""de"": ""madre"", ""probabilidad"": 0.1, ""al_anotar"": [""diario.cumpleanos""] },
              { ""id"": ""chat.d"", ""de"": ""madre"", ""probabilidad"": 0.1, ""si_no_respondes"": { ""carino"": -1 } },
              { ""id"": ""chat.e"", ""de"": ""amigo"", ""al_anotar"": [""evento.a""] }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("vecino"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("chat.b") && e.Message.Contains("probabilidad"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("chat.c") && e.Message.Contains("probabilidad"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("chat.d") && e.Message.Contains("si_no_respondes"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("evento.a"));
        }

        [Fact]
        public void TodoMensajeDelPaquete_EscuchaLineasQueAlgoAnotaDeVerdad()
        {
            var used = RepoPacks.Content.UsedTextKeys()
                .Where(k => !k.Value.StartsWith("mensaje ", System.StringComparison.Ordinal) && !k.Value.StartsWith("correo ", System.StringComparison.Ordinal))
                .Select(k => k.Key)
                .ToHashSet();
            var written = RepoPacks.Content.Events
                .SelectMany(e => e.Options.SelectMany(o => o.Effects).Concat(e.Effects))
                .SelectMany(effect => System.Text.RegularExpressions.Regex.Matches(effect, @"diario\('([^']+)'\)").Select(m => m.Groups[1].Value))
                .ToHashSet();

            Assert.NotEmpty(RepoPacks.Content.Chats);
            foreach (ChatDefinition chat in RepoPacks.Content.Chats)
            {
                foreach (string key in chat.OnJournal)
                {
                    Assert.True(used.Contains(key) || written.Contains(key), $"{chat.Id}: nadie anota '{key}' en el diario.");
                }
            }
        }

        [Fact]
        public void UnaVidaHastaLos25_RecibeMensajesDeGenteReal()
        {
            var session = new LifeSession(RepoPacks.Content, 1, "pais.chile");
            session.TimeMachine.Enabled = false;
            var life = new Presentation.LifePresenter(session);
            for (int step = 0; step < 3000 && life.Phase != Presentation.LifePhase.Over && session.PlayerAgeInYears < 25; step++)
            {
                if (life.Phase == Presentation.LifePhase.Living)
                {
                    life.AdvanceUntilSomething();
                }
                else if (life.Phase == Presentation.LifePhase.Choosing)
                {
                    life.Choose(0);
                }
                else
                {
                    break;
                }
            }

            ChatInbox inbox = session.World.Player.Chats!;
            Assert.NotNull(inbox);
            int messages = inbox.Chats.Sum(c => c.Messages.Count);
            Assert.True(inbox.Chats.Count >= 2 && messages >= 10, $"{inbox.Chats.Count} personas, {messages} mensajes.");
            foreach (Chat chat in inbox.Chats)
            {
                // Cada conversación es con alguien que existe en la vida del jugador.
                Assert.True(session.World.TryGetCharacter(chat.OtherId, out _));
            }
        }
    }
}

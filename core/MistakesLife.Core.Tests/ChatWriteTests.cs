using System.Reflection;
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
    public class ChatWriteTests
    {
        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        private static LifeSession Session(int age = 20)
        {
            var session = new LifeSession(RepoPacks.Content, 5, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            Run(session, "jugador.dar_celular()");
            Run(session, "jugador.instalar_app('app.mensajito')");
            return session;
        }

        private static void ChatWeeks(LifeSession session, int weeks) =>
            new WeekCycle().Register(new ChatSystem(session.Content)).AdvanceWeeks(session.World, weeks);

        private static Character Player(LifeSession session) => session.World.Player;

        private static Character NewFriend(LifeSession session)
        {
            Run(session, "jugador.conocer('amistad')");
            return RelationshipRules.WithBond(session.World, Player(session), RelationshipBond.Friend).OrderBy(c => c.Id).Last();
        }

        private static Relationship Feelings(LifeSession session, Character who) => RelationshipRules.Edit(session.World, who, Player(session));

        [Fact]
        public void EscribirleAUnAmigoQueTeQuiere_ContestaAlTiro_YSubeElCarino()
        {
            LifeSession session = Session();
            Character friend = NewFriend(session);
            Feelings(session, friend).Affection = 60;
            var life = new Presentation.LifePresenter(session);
            Assert.Contains(life.ChatContacts(), c => c.OtherId == friend.Id);
            Assert.Contains(life.Chat(friend.Id)!.Openers, o => o.Id == "escribir.amigo.juntarse");

            life.WriteChat(friend.Id, "escribir.amigo.juntarse");

            // Con el tiempo parado el chat es en vivo: contesta en ese minuto.
            Presentation.ChatView chat = life.Chat(friend.Id)!;
            Assert.False(chat.WaitingAnswer);
            Assert.False(chat.Messages.Last().FromPlayer);
            Assert.Equal(session.Text("escribir.amigo.juntarse"), chat.Messages[chat.Messages.Count - 2].Text);
            Assert.Equal(session.Text("escribir.amigo.juntarse.resp.dale"), chat.Messages.Last().Text);
            Assert.Equal(63, Feelings(session, friend).Affection);

            // Una vez por semana: no se le puede escribir otra cosa hasta que pase el tiempo.
            Assert.Empty(chat.Openers);
            Assert.Throws<System.InvalidOperationException>(() => session.WriteChat(friend.Id, "escribir.amigo.juntarse"));
            ChatWeeks(session, 1);
            chat = life.Chat(friend.Id)!;
            Assert.DoesNotContain(chat.Openers, o => o.Id == "escribir.amigo.juntarse");
        }

        [Fact]
        public void AlguienQueYaNoTeQuiere_TeDejaEnVisto()
        {
            LifeSession session = Session();
            Character friend = NewFriend(session);
            Feelings(session, friend).Affection = 10;
            var life = new Presentation.LifePresenter(session);

            life.WriteChat(friend.Id, "escribir.amigo.juntarse");

            Presentation.ChatView chat = life.Chat(friend.Id)!;
            Assert.True(chat.Messages.Last().FromPlayer);
            Assert.True(chat.LeftOnRead);
            Assert.Equal(10, Feelings(session, friend).Affection);
        }

        [Fact]
        public void PedirPerdon_SoloSiHayRencor_YLoBaja()
        {
            LifeSession session = Session();
            Character mother = session.World.GetCharacter(Player(session).MotherId!.Value);
            var life = new Presentation.LifePresenter(session);
            Assert.DoesNotContain(life.Chat(mother.Id)!.Openers, o => o.Id == "escribir.madre.perdon");

            Feelings(session, mother).Resentment = 30;
            Assert.Contains(life.Chat(mother.Id)!.Openers, o => o.Id == "escribir.madre.perdon");
            life.WriteChat(mother.Id, "escribir.madre.perdon");

            Assert.Equal(22, Feelings(session, mother).Resentment);
        }

        [Fact]
        public void SiTeEscribieronAlgo_PrimeroContestas_YNoSePuedeEscribirLoQueNoCorresponde()
        {
            LifeSession session = Session();
            Character mother = session.World.GetCharacter(Player(session).MotherId!.Value);
            ChatWeeks(session, 1);
            session.World.WriteJournal(Player(session).Id, "diario.cumpleanos");
            ChatWeeks(session, 1);

            Assert.Empty(new Presentation.LifePresenter(session).Chat(mother.Id)!.Openers);
            Assert.Throws<System.InvalidOperationException>(() => session.WriteChat(mother.Id, "escribir.madre.como_estas"));
            Assert.Throws<System.InvalidOperationException>(() => session.WriteChat(mother.Id, "escribir.amigo.juntarse"));
        }

        [Fact]
        public void PartidaGuardadaAntesDelChatEnVivo_LoQueEscribisteSeContestaLaSemanaSiguiente()
        {
            LifeSession session = Session();
            Character friend = NewFriend(session);
            Feelings(session, friend).Affection = 60;
            ChatWeeks(session, 1);
            // Así quedaba una partida vieja: lo escrito, esperando que la otra persona conteste.
            Chat chat = Player(session).Chats!.Open(friend.Id);
            typeof(Chat).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
                .Invoke(chat, new object[] { new ChatMessage(session.World.Date, "escribir.amigo.juntarse", fromPlayer: true) });
            typeof(Chat).GetProperty(nameof(Chat.PlayerWrote))!.SetValue(chat, "escribir.amigo.juntarse");
            typeof(Chat).GetProperty(nameof(Chat.PlayerWroteOn))!.SetValue(chat, session.World.Date);

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());
            Chat after = loaded.World.Player.Chats!.With(friend.Id)!;
            Assert.Equal("escribir.amigo.juntarse", after.PlayerWrote);
            Assert.Equal(session.World.Date, after.PlayerWroteOn);

            ChatWeeks(loaded, 1);
            Assert.Null(after.PlayerWrote);
            Assert.Equal("escribir.amigo.juntarse.resp.dale", after.Messages.Last().TextKey);
        }

        [Fact]
        public void MensajeParaEscribirMalArmado_SeDetecta()
        {
            var content = new ContentDatabase();
            content.AddChatOpenersJson("pack.prueba", "escribir.json", @"[
              { ""id"": ""escribir.a"", ""para"": ""vecino"", ""respuestas"": [ { ""id"": ""escribir.a.resp.x"" } ] },
              { ""id"": ""escribir.b"", ""para"": ""madre"", ""respuestas"": [] },
              { ""id"": ""hola"", ""para"": ""madre"", ""respuestas"": [ { ""id"": ""escribir.c.resp.x"", ""carino_minimo"": 80, ""carino_maximo"": 20 } ] }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("vecino"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("escribir.b") && e.Message.Contains("al menos una respuesta"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("hola") && e.Message.Contains("escribir."));
            Assert.Contains(ex.Errors, e => e.Source.Contains("hola") && e.Message.Contains("mínimo"));
        }
    }
}

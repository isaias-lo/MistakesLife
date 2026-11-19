using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// La gente que actúa por su cuenta (D4c): lo chico llega por Mensajito y sale de la vida
    /// real de quien escribe (tu mamá jubilada, tu amigo que busca pega, la vacante en la pega de
    /// tu amiga, su cumpleaños); lo grande para el tiempo (que te pidan ser aval).
    /// </summary>
    public class PeopleInitiativeTests
    {
        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        /// <summary>Una vida en Chile de esa edad, con celular y Mensajito.</summary>
        private static LifeSession Session(int age)
        {
            var session = new LifeSession(RepoPacks.Content, 5, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            Run(session, "jugador.dar_celular()");
            Run(session, "jugador.instalar_app('app.mensajito')");
            return session;
        }

        /// <summary>Corre solo Mensajito (y nada cambia en qué anda la gente).</summary>
        private static void ChatWeeks(LifeSession session, int weeks) =>
            new WeekCycle().Register(new ChatSystem(session.Content)).AdvanceWeeks(session.World, weeks);

        private static Character Player(LifeSession session) => session.World.Player;

        private static Character Mother(LifeSession session) => session.World.GetCharacter(Player(session).MotherId!.Value);

        private static IEnumerable<ChatMessage> From(LifeSession session, int otherId, string chatId) =>
            Player(session).Chats?.With(otherId)?.Messages.Where(m => !m.FromPlayer && m.TextKey == chatId) ?? Enumerable.Empty<ChatMessage>();

        private static Character NewFriend(LifeSession session)
        {
            Run(session, "jugador.conocer('amistad')");
            Character friend = RelationshipRules.WithBond(session.World, Player(session), RelationshipBond.Friend).OrderBy(c => c.Id).Last();
            RelationshipRules.Edit(session.World, friend, Player(session)).Affection = 80;
            return friend;
        }

        /// <summary>Le da a esa persona una de las pegas que existen enteras en el juego.</summary>
        private static void GiveJob(LifeSession session, Character person)
        {
            for (ulong seed = 1; person.Job == null; seed++)
            {
                person.WorkStatus = null;
                PeopleWorkSystem.Look(session.World, session.Content, session.Content.GetCountry("pais.chile").PeopleWork!, person, 40, new GameRandom(seed));
            }
        }

        [Fact]
        public void TuMamaSinPega_TeLoCuentaUnaVez_YSiTrabajaNo()
        {
            LifeSession session = Session(25);
            Mother(session).WorkStatus = WorkStatus.Working;
            ChatWeeks(session, 104);
            Assert.Empty(From(session, Mother(session).Id, "chat.madre.sin_pega"));

            Mother(session).WorkStatus = WorkStatus.Unemployed;
            ChatWeeks(session, 260);

            Assert.Single(From(session, Mother(session).Id, "chat.madre.sin_pega"));
        }

        [Fact]
        public void TuAmigaConPega_TeAvisaDeUnaVacante_YSiLeDicesQueSiTeRecomienda()
        {
            LifeSession session = Session(26);
            Player(session).Job = null;
            Character friend = NewFriend(session);
            GiveJob(session, friend);

            for (int week = 0; week < 520 && !From(session, friend.Id, "chat.amigo.vacante").Any(); week++)
            {
                ChatWeeks(session, 1);
            }

            Assert.NotEmpty(From(session, friend.Id, "chat.amigo.vacante"));
            session.ReplyChat(friend.Id, 0);

            Assert.Equal("busqueda.forma.contactos", Player(session).JobSearch!.MethodId);
            Assert.Equal(friend.Id, Player(session).JobSearch!.ContactId);
        }

        [Fact]
        public void LaInvitacionASuCumpleanos_LlegaLaSemanaDeSuCumpleanos_YNoTodosLosAnos()
        {
            LifeSession session = Session(20);
            Character friend = NewFriend(session);

            // Las invitaciones que van llegando (la conversación borra sus mensajes más antiguos).
            var invites = new List<ChatMessage>();
            for (int week = 0; week < 6 * GameDate.WeeksPerYear; week++)
            {
                ChatWeeks(session, 1);
                invites.AddRange(From(session, friend.Id, "chat.amigo.cumple_invita").Where(m => !invites.Any(i => i.Date == m.Date)));
                // Siempre contesta, para que pueda volver a escribir.
                if (Player(session).Chats?.With(friend.Id)?.AwaitingReply != null)
                {
                    session.ReplyChat(friend.Id, 0);
                }
            }

            Assert.InRange(invites.Count, 1, 2);
            Assert.All(invites, m => Assert.Equal(0, friend.AgeInWeeks(m.Date) % GameDate.WeeksPerYear));
        }

        [Fact]
        public void SiFirmasteDeAval_YTuAmigoNoPaga_TeTocaPagarATi()
        {
            LifeSession session = Session(30);
            Character friend = NewFriend(session);
            while (friend.Personality.Conscientiousness >= 40)
            {
                friend = NewFriend(session);
            }

            Effect.Parse("plantar('semilla.fuiste_aval', persona)").Apply(new EventScope(session.World, session.Content, Player(session), null, readOnly: false, friend));
            var seen = new List<string>();
            for (int week = 0; week < 2 * GameDate.WeeksPerYear && !seen.Contains("evento.gente.aval_cobra"); week++)
            {
                while (session.Pending != null)
                {
                    seen.Add(session.Pending.EventId);
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }

            Assert.Contains("evento.gente.aval_cobra", seen);
            Assert.DoesNotContain("evento.gente.aval_cumple", seen);
        }

        [Fact]
        public void AlGuardarYCargar_SeAcuerdaDeCuantasVecesLlegoCadaMensajeConTope()
        {
            LifeSession session = Session(25);
            Mother(session).WorkStatus = WorkStatus.Unemployed;
            ChatWeeks(session, 260);
            Assert.Equal(1, Player(session).Chats!.With(Mother(session).Id)!.LimitedSent["chat.madre.sin_pega"]);

            LifeSession loaded = LifeSession.LoadFromBytes(RepoPacks.Content, session.SaveToBytes());

            Assert.Equal(1, loaded.World.Player.Chats!.With(Mother(session).Id)!.LimitedSent["chat.madre.sin_pega"]);
        }

        [Fact]
        public void UnaRespuestaConEfectos_SeRevisaComoUnEvento()
        {
            var content = new Content.ContentDatabase();
            content.AddChatsJson("pack.prueba", "chats.json", @"[
              { ""id"": ""chat.amigo.x"", ""de"": ""amigo"", ""probabilidad"": 0.1, ""veces_por_persona"": 0,
                ""respuestas"": [ { ""id"": ""chat.amigo.x.resp"", ""efectos"": [ ""fantasma.carino += 1"" ] } ] }
            ]");

            var ex = Assert.Throws<Content.ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Source.Contains("chat.amigo.x") && e.Message.Contains("veces_por_persona"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("chat.amigo.x.resp") && e.Message.Contains("fantasma"));
        }
    }
}

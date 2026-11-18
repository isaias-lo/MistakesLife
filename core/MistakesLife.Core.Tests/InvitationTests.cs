using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Education;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// D4b: las amistades ya no nacen solas. Solo la de la infancia llega sola; las demás nacen
    /// de una invitación (estudiar juntos, un café después de la pega, un cumpleaños) que el
    /// jugador acepta.
    /// </summary>
    public class InvitationTests
    {
        /// <summary>Una vida en Chile hasta esa edad, eligiendo siempre la primera opción.</summary>
        private static LifeSession Life(ulong seed, int age)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            while (session.World.Player.IsAlive && (session.PlayerAgeInYears < age || session.Pending != null))
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }
                else
                {
                    session.Advance(1);
                }
            }

            return session;
        }

        /// <summary>Juega semana a semana hasta que ese momento llegue (contestando los otros con la primera opción).</summary>
        private static bool UntilEvent(LifeSession session, string eventId, int maxWeeks)
        {
            for (int week = 0; week < maxWeeks && session.World.Player.IsAlive; week++)
            {
                while (session.Pending != null)
                {
                    if (session.Pending.EventId == eventId)
                    {
                        return true;
                    }

                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }

            return session.Pending?.EventId == eventId;
        }

        /// <summary>Hace que el jugador y alguien de su curso se quieran de verdad (química de ida y vuelta).</summary>
        private static Character Chemistry(World world, Character person)
        {
            RelationshipRules.Edit(world, world.Player, person).Affection = 75;
            RelationshipRules.Edit(world, person, world.Player).Affection = 75;
            return person;
        }

        [Fact]
        public void LaAmistadDeLaInfancia_LlegaSolaUnaSolaVez()
        {
            LifeSession session = Life(3, 7);
            World world = session.World;
            Character player = world.Player;
            player.ClearFlag(SchoolRules.ChildhoodFriendFlag);
            var free = SchoolRules.Classmates(world, player).Where(m => RelationshipRules.Feelings(world, player, m).Bond == RelationshipBond.None).ToList();

            Chemistry(world, free[0]);
            Assert.Equal(free[0], SchoolRules.ChildhoodFriendship(world, player));
            Assert.Equal(RelationshipBond.Friend, RelationshipRules.Feelings(world, player, free[0]).Bond);

            Chemistry(world, free[1]);
            Assert.Null(SchoolRules.ChildhoodFriendship(world, player));
            Assert.Equal(RelationshipBond.None, RelationshipRules.Feelings(world, player, free[1]).Bond);
        }

        [Fact]
        public void ConQuimicaEnElCurso_TeInvitan_YSiVasNaceLaAmistad()
        {
            LifeSession session = Life(4, 13);
            World world = session.World;
            Character mate = SchoolRules.Classmates(world, world.Player).First(m => RelationshipRules.Feelings(world, world.Player, m).Bond == RelationshipBond.None);
            Chemistry(world, mate);

            // Se busca con la química puesta cada semana, por si otro momento la movió.
            bool invited = false;
            for (int i = 0; i < 52 && !invited; i++)
            {
                Chemistry(world, mate);
                invited = UntilEvent(session, "evento.invitacion.colegio", 1) && session.Pending!.PersonId == mate.Id;
            }

            Assert.True(invited);
            session.Choose(0);
            Assert.Equal(RelationshipBond.Friend, RelationshipRules.Feelings(world, world.Player, mate).Bond);
        }

        [Fact]
        public void SiNoVas_NoNaceLaAmistad()
        {
            LifeSession session = Life(4, 13);
            World world = session.World;
            Character mate = SchoolRules.Classmates(world, world.Player).First(m => RelationshipRules.Feelings(world, world.Player, m).Bond == RelationshipBond.None);

            bool invited = false;
            for (int i = 0; i < 52 && !invited; i++)
            {
                Chemistry(world, mate);
                invited = UntilEvent(session, "evento.invitacion.colegio", 1) && session.Pending!.PersonId == mate.Id;
            }

            Assert.True(invited);
            session.Choose(1);
            Assert.NotEqual(RelationshipBond.Friend, RelationshipRules.Feelings(world, world.Player, mate).Bond);
        }

        [Fact]
        public void SinQuimica_NadieTeInvita()
        {
            LifeSession session = Life(4, 13);
            World world = session.World;
            foreach (Character mate in SchoolRules.Classmates(world, world.Player))
            {
                RelationshipRules.Edit(world, mate, world.Player).Affection = 10;
            }

            var scope = new Events.EventScope(world, session.Content, world.Player, null, readOnly: true, SchoolRules.Classmates(world, world.Player)[0]);
            Assert.False(Scripting.Expression.Parse("persona.quiere_amistad").IsTrue(scope));
        }

        [Fact]
        public void EnUnaVidaEntera_CadaAmistadDelCursoOLaPega_NaceDeUnaInvitacionAceptada_SalvoLaDeLaInfancia()
        {
            LifeSession session = Life(5, 35);
            World world = session.World;
            int accepted = world.EventHistory.Count(r => r.EventId.StartsWith("evento.invitacion.") && r.ChosenOption == 0);
            int lines = world.Journal.Count(e => e.CharacterId == world.PlayerId && (e.TextKey == "diario.curso.amistad" || e.TextKey == "diario.pega.amistad"));
            int childhood = world.Player.HasFlag(SchoolRules.ChildhoodFriendFlag) ? 1 : 0;

            Assert.True(accepted > 0);
            Assert.Equal(accepted + childhood, lines);
        }
    }
}

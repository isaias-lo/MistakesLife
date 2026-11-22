using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5a3b: los talleres del colegio y el centro de alumnos, a pedido desde Estudios y en momentos.</summary>
    public class SchoolWorkshopsAndCouncilTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida jugada hasta tener clases a esa edad, en los meses en que se inscriben listas (marzo a mayo).</summary>
        private static LifeSession InClassInAutumn(int age, ulong seed = 5)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            Character player = session.World.Player;
            while (player.IsAlive && (player.AgeInYears(session.World.Date) < age || !SchoolRules.InClasses(session.World, Content, player)
                || session.World.Date.Month < 3 || session.World.Date.Month > 4 || session.Pending != null))
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(1);
            }

            player.Routine = new Routine("libre");
            return session;
        }

        private static void Choose(LifeSession session, string optionKey)
        {
            PendingEvent pending = session.Pending!;
            EventDefinition ev = Content.Events.Single(e => e.Id == pending.EventId);
            session.Choose(pending.AvailableOptions.Single(i => ev.Options[i].TextKey == optionKey));
        }

        private static void PlayWeeks(LifeSession session, int weeks)
        {
            for (int week = 0; week < weeks; week++)
            {
                while (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }
        }

        [Fact]
        public void EnEstudios_AparecenTallerYCentroDeAlumnos()
        {
            LifeSession session = InClassInAutumn(14);
            var buttons = new LifePresenter(session).StudyActions().Select(a => a.EventId).ToList();

            Assert.Contains("evento.colegio.pedir_taller", buttons);
            Assert.Contains("evento.colegio.pedir_centro", buttons);
        }

        [Fact]
        public void Taller_SeInscribe_OcupaDosTardes_YQuedaSuMarca()
        {
            LifeSession session = InClassInAutumn(12);
            Character player = session.World.Player;

            Assert.True(session.Request("evento.colegio.pedir_taller"));
            Choose(session, "opcion.colegio.pedir_taller.futbol");

            Assert.Equal("futbol", player.SchoolRecord!.WorkshopId);
            Assert.True(player.HasFlag("hizo_taller_futbol"));
            Assert.Equal(WorkshopRules.AfternoonsPerWeek, player.Routine!.Count("actividad.taller.futbol"));
            Assert.DoesNotContain(new LifePresenter(session).StudyActions(), a => a.EventId == "evento.colegio.pedir_taller");
        }

        [Fact]
        public void CentroDeAlumnos_DespuesDeLaCampana_VotaElColegio()
        {
            LifeSession session = InClassInAutumn(15);
            Character player = session.World.Player;

            Assert.True(session.Request("evento.colegio.pedir_centro"));
            Choose(session, "opcion.colegio.pedir_centro.tesoreria");
            SchoolRecord record = player.SchoolRecord!;
            Assert.Equal("tesoreria", record.CouncilRole);
            Assert.Null(record.CouncilResult);

            PlayWeeks(session, StudentCouncilRules.CampaignWeeks + 2);

            Assert.Contains(record.CouncilResult, new[] { "gano", "perdio" });
            Assert.Equal(record.CouncilResult == "gano", player.HasFlag("fue_centro_alumnos"));
            Assert.Contains(session.World.Journal, e => e.CharacterId == player.Id && e.TextKey.StartsWith("diario.centro.", System.StringComparison.Ordinal)
                && e.TextKey != "diario.centro.postulo");
        }

        [Fact]
        public void CentroDeAlumnos_GanaMasQuienEsQuerido_YLaPresidenciaCuestaMas()
        {
            LifeSession session = InClassInAutumn(15);
            Character player = session.World.Player;
            var classmates = SchoolRules.Classmates(session.World, player);
            foreach (Character classmate in classmates)
            {
                classmate.RelationshipWith(player.Id)!.Affection = 10;
            }

            double disliked = StudentCouncilRules.WinChance(session.World, player, "tesoreria");
            foreach (Character classmate in classmates)
            {
                classmate.RelationshipWith(player.Id)!.Affection = 90;
            }

            double loved = StudentCouncilRules.WinChance(session.World, player, "tesoreria");
            Assert.True(loved > disliked + 0.3);
            Assert.True(StudentCouncilRules.WinChance(session.World, player, "presidencia") < loved);
        }

        [Fact]
        public void TallerYCentro_SonDeUnAno_SeBorranAlCerrarElAno()
        {
            LifeSession session = InClassInAutumn(14);
            Character player = session.World.Player;
            Assert.True(session.Request("evento.colegio.pedir_taller"));
            Choose(session, "opcion.colegio.pedir_taller.musica");
            Assert.True(session.Request("evento.colegio.pedir_centro"));
            Choose(session, "opcion.colegio.pedir_centro.secretaria");

            int year = session.World.Date.Year;
            while (player.IsAlive && session.World.Date.Year == year)
            {
                PlayWeeks(session, 1);
            }

            PlayWeeks(session, 2);
            Assert.Null(player.SchoolRecord!.WorkshopId);
            Assert.Null(player.SchoolRecord.CouncilRole);
            Assert.True(player.HasFlag("hizo_taller_musica"), "lo que hizo en el taller queda para después.");
        }
    }
}

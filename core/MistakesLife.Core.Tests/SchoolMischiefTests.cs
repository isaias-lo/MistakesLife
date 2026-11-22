using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Simulation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5a3a: copiar en la prueba y hacer la cimarra, a pedido desde Estudios y en momentos con tu curso.</summary>
    public class SchoolMischiefTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static CountryDefinition Chile => Content.GetCountry("pais.chile");

        /// <summary>Una vida jugada hasta tener clases a esa edad (y, si se pide, con una prueba anunciada).</summary>
        private static LifeSession InClass(int age, bool withTest, ulong seed = 5)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            Character player = session.World.Player;
            while (player.IsAlive && (player.AgeInYears(session.World.Date) < age || !SchoolRules.InClasses(session.World, Content, player)
                || StudyRules.Sets(session.World, Content, player).Count == 0 || (withTest && !(player.SchoolRecord?.TestPending ?? false))
                || session.Pending != null))
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

        [Fact]
        public void EnEstudios_AparecenCopiarYCimarra_YNoEnLaLineaDeCadaCompanero()
        {
            LifeSession session = InClass(14, withTest: true);
            var presenter = new LifePresenter(session);

            var buttons = presenter.StudyActions().Select(a => a.EventId).ToList();
            Assert.Contains("evento.colegio.pedir_copiar", buttons);
            Assert.Contains("evento.colegio.pedir_cimarra", buttons);

            Character classmate = SchoolRules.Classmates(session.World, session.World.Player).First();
            Assert.DoesNotContain(presenter.PersonActions(classmate.Id), a => a.EventId.StartsWith("evento.colegio.", System.StringComparison.Ordinal));
        }

        [Fact]
        public void Copiar_LaPruebaSeRinde_ConNotaMinimaSiTePillan_ONotaQueNoVieneDelDominio()
        {
            LifeSession session = InClass(14, withTest: true);
            Character player = session.World.Player;
            SchoolRecord record = player.SchoolRecord!;
            string subject = record.TestSubjectId!;
            int masteryBefore = StudyRules.Mastery(player, subject) ?? 0;

            Assert.True(session.Request("evento.colegio.pedir_copiar"));
            Choose(session, "opcion.colegio.pedir_copiar.torpedo");
            Assert.True(record.TestCheating);
            Assert.DoesNotContain(new LifePresenter(session).StudyActions(), a => a.EventId == "evento.colegio.pedir_copiar");

            for (int week = 0; week < 4 && !record.TestMark.HasValue; week++)
            {
                while (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }

            Assert.True(record.TestMark.HasValue);
            Assert.False(record.TestCheating);
            if (record.TestCaught)
            {
                Assert.Equal(Chile.GradeScale!.Min, record.TestMark!.Value);
                Assert.True(player.HasFlag("pillado_copiando"));
            }
            else
            {
                Assert.False(player.HasFlag("pillado_copiando"));
            }

            Assert.True((StudyRules.Mastery(player, subject) ?? 0) <= masteryBefore + 10, "copiar no enseña: el dominio no salta.");
        }

        [Fact]
        public void Copiar_SinQueTePillen_SubeUnPunto_SinPasarseDelSiete()
        {
            GradeScaleDefinition scale = Chile.GradeScale!;
            Assert.Equal(5.2, GradeRules.Copied(4.2, scale), 3);
            Assert.Equal(scale.Max, GradeRules.Copied(6.6, scale), 3);
        }

        [Fact]
        public void Cimarra_SumaUnaFalta_YSePierdeLaClase()
        {
            LifeSession session = InClass(14, withTest: false);
            Character player = session.World.Player;
            SchoolRecord record = player.SchoolRecord!;
            var before = record.Mastery.ToDictionary(m => m.Key, m => m.Value);

            Assert.True(session.Request("evento.colegio.pedir_cimarra"));
            Choose(session, "opcion.colegio.pedir_cimarra.plaza");

            Assert.Equal(1, record.Absences);
            Assert.True(player.HasFlag("hizo_cimarra"));
            Assert.All(before, m => Assert.Equal(System.Math.Max(0, m.Value - GradeRules.MasteryLostPerAbsence), record.Mastery[m.Key]));
            Assert.Contains(session.World.Seeds.OpenFor(player.Id), s => s.TypeId == "semilla.cimarra_descubierta");
        }

        [Fact]
        public void AsistenciaMinima_ConMasDe15PorCientoDeFaltas_SeRepite()
        {
            EducationStageDefinition media = Chile.EducationStages.Single(e => e.Id == "etapa.chile.media");
            int days = Chile.SchoolWeeksPerYear() * GradeRules.SchoolDaysPerWeek;
            Assert.Equal(0.85, media.MinAttendance);
            Assert.True(GradeRules.MeetsAttendance(media, Chile, (int)(days * 0.15)));
            Assert.False(GradeRules.MeetsAttendance(media, Chile, (int)(days * 0.15) + 2));
        }
    }
}

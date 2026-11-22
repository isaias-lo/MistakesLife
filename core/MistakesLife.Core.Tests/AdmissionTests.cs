using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5a5: la PAES de Chile con NEM, ranking, pruebas, cortes por carrera y preuniversitario.</summary>
    public class AdmissionTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private const string Civil = "carrera.software.nivel.ingenieria_civil_computacion";
        private const string Auditor = "carrera.contabilidad.nivel.contador_auditor";

        /// <summary>Una vida en Chile jugada (eligiendo la primera opción) hasta que llega ese momento.</summary>
        private static LifeSession PlayUntil(string eventId, ulong seed = 7)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            for (int week = 0; week < 52 * 20 && session.World.Player.IsAlive; week++)
            {
                while (session.Pending != null && session.Pending.EventId != eventId)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                if (session.Pending != null)
                {
                    return session;
                }

                session.Advance(1);
            }

            Assert.Fail($"No llegó {eventId}.");
            return session;
        }

        private static void Choose(LifeSession session, string optionKey)
        {
            PendingEvent pending = session.Pending!;
            EventDefinition ev = Content.GetEvent(pending.EventId);
            session.Choose(pending.AvailableOptions.Single(i => ev.Options[i].TextKey == optionKey));
        }

        /// <summary>Un jugador de 18 en Chile con estos puntajes, para revisar los cortes sin jugar la vida.</summary>
        private static Character WithScores(int nem, int ranking, int lectora, int matematica)
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            Character player = session.World.Player;
            var record = player.SchoolRecord ??= new SchoolRecord();
            record.AdmissionNem = nem;
            record.AdmissionRanking = ranking;
            record.AdmissionTests["lectora"] = lectora;
            record.AdmissionTests["matematica"] = matematica;
            record.AdmissionAttempts = 1;
            return player;
        }

        [Fact]
        public void Nem_Y_Ranking_SalenDelPromedioComoEnElDemre()
        {
            Assert.Equal(100, AdmissionRules.Nem(4.0));
            Assert.Equal(550, AdmissionRules.Nem(5.5));
            Assert.Equal(1000, AdmissionRules.Nem(7.0));

            // Bajo el promedio del colegio, el ranking es igual al NEM; sobre él, sube hacia 1.000.
            Assert.Equal(550, AdmissionRules.Ranking(550, 5.5, 5.6));
            int nem = AdmissionRules.Nem(6.3);
            int ranking = AdmissionRules.Ranking(nem, 6.3, 5.6);
            Assert.True(ranking > nem && ranking < 1000);
            Assert.Equal(1000, AdmissionRules.Ranking(1000, 7.0, 5.6));
        }

        [Fact]
        public void Prueba_SubeConLoQueDominas_YConElPreu()
        {
            int weak = AdmissionRules.TestScore(mastery: 35, smarts: 40, preparation: 0, day: 0);
            int strong = AdmissionRules.TestScore(mastery: 85, smarts: 60, preparation: 0, day: 0);
            int prepared = AdmissionRules.TestScore(mastery: 35, smarts: 40, preparation: 2, day: 0);

            Assert.True(weak < 458, $"débil {weak}");
            Assert.True(strong > 750, $"fuerte {strong}");
            Assert.Equal(weak + (2 * AdmissionRules.PointsPerPreparation), prepared);
        }

        [Fact]
        public void Corte_CadaCarreraPonderaASuManera_YLaUniversidadPide458()
        {
            // Bueno en Matemática, flojo en Lectora: le alcanza para Contador Auditor y no para Civil en Computación.
            Character player = WithScores(nem: 600, ranking: 640, lectora: 470, matematica: 650);
            Assert.True(AdmissionRules.Reaches(Content, player, Auditor));
            Assert.False(AdmissionRules.Reaches(Content, player, Civil));
            Assert.True(AdmissionRules.Reaches(Content, player, "carrera.software.nivel.analista_programador")); // el instituto no pide puntaje

            Character top = WithScores(nem: 800, ranking: 900, lectora: 700, matematica: 820);
            Assert.True(AdmissionRules.Reaches(Content, top, Civil));

            // Con ponderado alto pero promedio Lectora-Matemática bajo 458, no puede postular a la universidad.
            Character low = WithScores(nem: 1000, ranking: 1000, lectora: 300, matematica: 600);
            Assert.False(AdmissionRules.Reaches(Content, low, Auditor));
        }

        [Fact]
        public void OtraVez_ValeElMejorDeCadaPrueba_YElNemNoCambia()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            Character player = session.World.Player;
            player.SchoolRecord = new SchoolRecord();
            player.SchoolRecord.YearAverages.AddRange(new[] { 5.8, 6.0, 6.1 });
            player.SchoolRecord.Mastery["ramo.chile.lengua_literatura"] = 40;
            player.SchoolRecord.Mastery["ramo.chile.matematica"] = 80;

            AdmissionRules.Take(session.World, Content, player, 0);
            int nem = player.SchoolRecord.AdmissionNem;
            int math = player.SchoolRecord.AdmissionTests["matematica"];
            Assert.Equal(AdmissionRules.Nem(5.966666666666667), nem);

            player.SchoolRecord.Mastery["ramo.chile.lengua_literatura"] = 75;
            player.SchoolRecord.Mastery["ramo.chile.matematica"] = 30;
            player.SchoolRecord.YearAverages.Add(4.0);
            AdmissionRules.Take(session.World, Content, player, 0);

            Assert.Equal(2, player.SchoolRecord.AdmissionAttempts);
            Assert.Equal(nem, player.SchoolRecord.AdmissionNem);
            Assert.Equal(math, player.SchoolRecord.AdmissionTests["matematica"]); // la segunda vez le fue peor: queda la primera
            Assert.True(player.SchoolRecord.LastAdmissionTests["matematica"] < math);
            Assert.True(player.SchoolRecord.AdmissionTests["lectora"] > AdmissionRules.TestScore(40, player.Stats.Smarts, 0, AdmissionRules.DayNoise));
        }

        [Fact]
        public void Paes_SeDaEnNoviembreDeCuartoMedio_YLosResultadosLleganEnEnero()
        {
            LifeSession session = PlayUntil("evento.prueba_paes");
            Character player = session.World.Player;
            Assert.Equal(4, player.SchoolClass!.Level);
            Assert.Equal(11, session.Pending!.Date.Month);

            Choose(session, "opcion.paes.rendir");
            Assert.Equal(1, player.SchoolRecord!.AdmissionAttempts);
            Assert.True(player.SchoolRecord.AdmissionNem > 100);
            Assert.Equal(2, player.SchoolRecord.AdmissionTests.Count);

            string results = player.SchoolRecord.AdmissionTests.Values.Average() >= 458 ? "evento.resultados_paes_altos" : "evento.resultados_paes_medios";
            for (int week = 0; week < 12 && session.Pending?.EventId != results; week++)
            {
                while (session.Pending != null && session.Pending.EventId != results)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                if (session.Pending == null)
                {
                    session.Advance(1);
                }
            }

            Assert.Equal(results, session.Pending?.EventId);
            Assert.Equal(1, session.Pending!.Date.Month);
            string text = new LifePresenter(session).Snapshot().PendingEvent!.Text;
            Assert.Contains("NEM " + player.SchoolRecord.AdmissionNem, text);
            Assert.Contains("Matemática " + player.SchoolRecord.LastAdmissionTests["matematica"], text);
        }

        [Fact]
        public void Preu_UnAnoDespues_DaLaPaesOtraVez_YPostula()
        {
            LifeSession session = PlayUntil("evento.prueba_paes", seed: 11);
            Character player = session.World.Player;
            Choose(session, "opcion.paes.rendir");
            int firstMath = player.SchoolRecord!.AdmissionTests["matematica"];

            session = Continue(session, e => e.StartsWith("evento.resultados_paes_", System.StringComparison.Ordinal));
            Choose(session, "opcion.futuro.preu");
            Assert.True(player.HasFlag("preparando_paes"));
            Assert.Equal("servicio.preu.pagado", player.Services["servicio.preu"]);

            session = Continue(session, e => e == "evento.paes.otra_vez");
            Assert.Equal(11, session.Pending!.Date.Month);
            Choose(session, "opcion.paes.otra_vez.rendir");
            Assert.Equal(2, player.SchoolRecord.AdmissionAttempts);
            Assert.True(player.SchoolRecord.AdmissionTests["matematica"] >= firstMath);
            Assert.False(player.Services.ContainsKey("servicio.preu"));

            session = Continue(session, e => e == "evento.paes.resultados_otra_vez");
            Assert.Equal(1, session.Pending!.Date.Month);
            Assert.True(player.HasFlag("resultados_paes")); // así los resultados de la primera vez no vuelven a salir
        }

        [Fact]
        public void Guardado_LlevaLosPuntajesDeLaPaes()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            Character player = session.World.Player;
            player.SchoolRecord = new SchoolRecord();
            player.SchoolRecord.Mastery["ramo.chile.matematica"] = 70;
            AdmissionRules.Take(session.World, Content, player, 1);

            Character loaded = LifeSession.Load(Content, session.SaveToText()).World.Player;

            Assert.Equal(player.SchoolRecord.AdmissionNem, loaded.SchoolRecord!.AdmissionNem);
            Assert.Equal(player.SchoolRecord.AdmissionRanking, loaded.SchoolRecord.AdmissionRanking);
            Assert.Equal(player.SchoolRecord.AdmissionTests, loaded.SchoolRecord.AdmissionTests);
            Assert.Equal(player.SchoolRecord.LastAdmissionTests, loaded.SchoolRecord.LastAdmissionTests);
            Assert.Equal(1, loaded.SchoolRecord.AdmissionAttempts);
        }

        private static LifeSession Continue(LifeSession session, System.Func<string, bool> until)
        {
            for (int week = 0; week < 60 && session.World.Player.IsAlive; week++)
            {
                while (session.Pending != null && !until(session.Pending.EventId))
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                if (session.Pending != null)
                {
                    return session;
                }

                session.Advance(1);
            }

            Assert.Fail("No llegó el momento esperado.");
            return session;
        }
    }
}

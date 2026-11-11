using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class PhoneTests
    {
        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        private static Value Eval(LifeSession session, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true));

        private static LifeSession Session(ulong seed = 5)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            return session;
        }

        [Fact]
        public void DarCelular_TraeSoloLasAppsDeFabrica_YSePuedenInstalarOtras()
        {
            LifeSession session = Session();
            Assert.False(Eval(session, "jugador.tiene_celular").AsBool("prueba"));

            Run(session, "jugador.dar_celular()");

            Assert.True(Eval(session, "jugador.tiene_celular").AsBool("prueba"));
            Assert.Equal(new[] { "app.correo" }, session.World.Player.Phone!.Apps);
            Assert.False(Eval(session, "jugador.tiene_app('app.instante')").AsBool("prueba"));

            Run(session, "jugador.instalar_app('app.instante')");
            Run(session, "jugador.instalar_app('app.instante')");
            Assert.True(Eval(session, "jugador.tiene_app('app.instante')").AsBool("prueba"));
            Assert.Equal(2, session.World.Player.Phone.Apps.Count);

            Run(session, "jugador.desinstalar_app('app.instante')");
            Assert.False(Eval(session, "jugador.tiene_app('app.instante')").AsBool("prueba"));
        }

        [Fact]
        public void InstalarUnaApp_SinCelular_DaUnErrorClaro()
        {
            LifeSession session = Session();
            var ex = Assert.Throws<ScriptException>(() => Run(session, "jugador.instalar_app('app.instante')"));
            Assert.Contains("no tiene celular", ex.Message);
        }

        [Fact]
        public void PerderElCelular_SeVanSusApps_PeroElCorreoSigue()
        {
            LifeSession session = Session();
            Run(session, "jugador.dar_celular()");
            Run(session, "jugador.instalar_app('app.mensajito')");
            Run(session, "jugador.crear_correo()");

            Run(session, "jugador.perder_celular()");

            Assert.False(Eval(session, "jugador.tiene_celular").AsBool("prueba"));
            Assert.False(Eval(session, "jugador.tiene_app('app.mensajito')").AsBool("prueba"));
            Assert.True(Eval(session, "jugador.tiene_correo").AsBool("prueba"));
        }

        [Fact]
        public void Correo_SoloRecibeLosAvisosDeLoQuePasaDespuesDeCrearlo()
        {
            LifeSession session = Session();
            int player = session.World.PlayerId;
            session.World.WriteJournal(player, JobSearchRules.RejectionJournal);
            Run(session, "jugador.crear_correo()");
            MailSystem.Deliver(session.World, session.Content);
            Assert.Empty(session.World.Player.Mail!.Messages);

            session.World.WriteJournal(player, JobSearchRules.RejectionJournal);
            session.World.WriteJournal(player, "diario.cumpleanos");
            MailSystem.Deliver(session.World, session.Content);
            MailSystem.Deliver(session.World, session.Content);

            MailMessage message = Assert.Single(session.World.Player.Mail.Messages);
            Assert.Equal("correo.busqueda.rechazo", message.MailId);
            Assert.False(message.Read);
            Assert.Equal(session.World.Date, message.Date);
        }

        [Fact]
        public void SinCorreo_ElAvisoNoLlega_PeroQuedaEnElDiario()
        {
            LifeSession session = Session();
            session.World.WriteJournal(session.World.PlayerId, JobSearchRules.RejectionJournal);

            MailSystem.Deliver(session.World, session.Content);
            Run(session, "jugador.recibir_correo('correo.estafa.banco')");

            Assert.Null(session.World.Player.Mail);
            Assert.Contains(session.World.Journal, j => j.TextKey == JobSearchRules.RejectionJournal);
        }

        [Fact]
        public void RecibirCorreo_LlegaSinLeer_YAlAbrirloQuedaLeido()
        {
            LifeSession session = Session();
            Run(session, "jugador.crear_correo()");
            Run(session, "jugador.recibir_correo('correo.bienvenida')");
            Assert.Equal(1.0, Eval(session, "jugador.correos_sin_leer").AsNumber("prueba"));

            session.ReadMail(0);

            Assert.Equal(0.0, Eval(session, "jugador.correos_sin_leer").AsNumber("prueba"));
            Assert.Equal(1.0, Eval(session, "jugador.correos").AsNumber("prueba"));
        }

        [Fact]
        public void LlamadoAEntrevista_LlegaAlCorreoDeQuienBuscaTrabajo()
        {
            LifeSession session = Session(3);
            new WeekCycle().AdvanceWeeks(session.World, 25 * Time.GameDate.WeeksPerYear);
            Run(session, "jugador.crear_correo()");
            Run(session, "jugador.marcar('buscando_trabajo')");
            Run(session, "jugador.buscar_trabajo('busqueda.forma.portales')");
            WeekCycle cycle = new WeekCycle().Register(new JobSearchSystem(session.Content)).Register(new MailSystem(session.Content));

            for (int week = 0; week < 500 && session.World.Player.JobSearch!.Stage != JobSearchStage.Interview; week++)
            {
                cycle.AdvanceWeek(session.World);
            }

            Assert.Equal(JobSearchStage.Interview, session.World.Player.JobSearch!.Stage);
            Assert.Equal("correo.busqueda.llamado", Assert.Single(session.World.Player.Mail!.Messages).MailId);
        }

        [Fact]
        public void Bandeja_AlLlenarse_BorraPrimeroElCorreoLeidoMasAntiguo()
        {
            var mail = new MailAccount(new Time.GameDate(0), 0);
            mail.Receive(new Time.GameDate(1), "correo.uno");
            mail.Receive(new Time.GameDate(2), "correo.dos");
            mail.MarkRead(1);
            for (int i = 0; i < MailAccount.MaxMessages - 1; i++)
            {
                mail.Receive(new Time.GameDate(3), "correo.otro");
            }

            Assert.Equal(MailAccount.MaxMessages, mail.Messages.Count);
            Assert.Equal("correo.uno", mail.Messages[0].MailId);
            Assert.DoesNotContain(mail.Messages, m => m.MailId == "correo.dos");
        }

        [Fact]
        public void Guardar_ConCelularYCorreo_ConservaLasAppsYLaBandeja()
        {
            LifeSession session = Session();
            Run(session, "jugador.dar_celular()");
            Run(session, "jugador.instalar_app('app.flechazo')");
            Run(session, "jugador.crear_correo()");
            Run(session, "jugador.recibir_correo('correo.bienvenida')");
            Run(session, "jugador.recibir_correo('correo.estafa.banco')");
            session.ReadMail(0);

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.Equal(session.World.Player.Phone!.Apps, loaded.World.Player.Phone!.Apps);
            Assert.Equal(session.World.Player.Phone.Since, loaded.World.Player.Phone.Since);
            MailAccount before = session.World.Player.Mail!;
            MailAccount after = loaded.World.Player.Mail!;
            Assert.Equal(before.JournalCursor, after.JournalCursor);
            Assert.Equal(before.Messages.Select(m => (m.MailId, m.Read, m.Date)), after.Messages.Select(m => (m.MailId, m.Read, m.Date)));
        }

        [Fact]
        public void AppOCorreoQueNoExisten_SeDetectanAlCargar()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events.json", @"[
              { ""id"": ""evento.a"", ""efectos"": [""jugador.instalar_app('app.no_existe')"", ""jugador.recibir_correo('correo.no_existe')""] }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("app.no_existe"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("correo.no_existe"));
        }

        [Fact]
        public void CorreoConTemaInventadoOLineaQueNoEsDelDiario_SeDetecta()
        {
            var content = new ContentDatabase();
            content.AddMailJson("pack.prueba", "mail.json", @"[
              { ""id"": ""correo.a"", ""remitente"": ""remitente.x"", ""tema"": ""chisme"", ""al_anotar"": [""evento.a""] }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("chisme"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("evento.a"));
        }

        [Fact]
        public void TodoCorreoDelPaquete_EscuchaLineasQueAlgoAnotaDeVerdad()
        {
            var used = RepoPacks.Content.UsedTextKeys()
                .Where(k => !k.Value.StartsWith("correo ", System.StringComparison.Ordinal))
                .Select(k => k.Key)
                .ToHashSet();
            var written = RepoPacks.Content.Events
                .SelectMany(e => e.Options.SelectMany(o => o.Effects).Concat(e.Effects))
                .SelectMany(effect => System.Text.RegularExpressions.Regex.Matches(effect, @"diario\('([^']+)'\)").Select(m => m.Groups[1].Value))
                .ToHashSet();

            foreach (MailDefinition mail in RepoPacks.Content.Mails)
            {
                foreach (string key in mail.OnJournal)
                {
                    Assert.True(used.Contains(key) || written.Contains(key), $"{mail.Id}: nadie anota '{key}' en el diario.");
                }
            }
        }
    }
}

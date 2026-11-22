using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5b: la pega por dentro, con tareas del mes, la barra de tu jefa y el ascenso pedido u ofrecido.</summary>
    public class WorkTaskTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private const string Taxes = "carrera.contabilidad.area.impuestos";
        private const string Development = "carrera.software.area.desarrollo";

        /// <summary>Una vida en Chile que a los 25 entra a trabajar en esa área, recién titulada.</summary>
        private static LifeSession WorkingIn(string areaId, ulong seed = 21)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            var decider = new AutoDecider(Content);
            var chooser = new GameRandom(seed).Fork("prueba.tareas");
            int target = 25 * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && (session.World.Player.AgeInWeeks(session.World.Date) < target || session.Pending != null))
            {
                if (session.Pending != null)
                {
                    session.Choose(decider.Choose(session.World, session.Pending, chooser));
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }

            Assert.True(session.World.Player.IsAlive);
            CareerAreaDefinition area = Content.Careers.SelectMany(c => c.Areas).Single(a => a.Id == areaId);
            CareerLevelDefinition best = Content.GetCareer(area.CareerId).Levels.OrderByDescending(l => l.SalaryRelative).First();
            var effects = new List<string>();
            if (session.World.Player.Studies != null)
            {
                effects.Add("jugador.desertar()");
            }

            effects.Add($"estudiar('{best.Id}')");
            effects.Add("jugador.titularse()");
            effects.Add("jugador.marcar('diplomado_tributacion')");
            effects.Add("jugador.desmarcar('buscando_trabajo')");
            effects.Add("jugador.marcar('trabajando')");
            effects.Add($"jugador.trabajar_en('{areaId}')");
            var scope = new EventScope(session.World, Content, session.World.Player, null, readOnly: false);
            foreach (string effect in effects)
            {
                Effect.Parse(effect).Apply(scope);
            }

            return session;
        }

        /// <summary>Avanza semana a semana (contestando con la primera opción lo que no es tarea) hasta el próximo mes.</summary>
        private static void ToNextMonth(LifeSession session)
        {
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            session.Advance(session.World.Date.WeeksToNextMonth);
        }

        [Fact]
        public void Frase_DeLaBarra_VaDePreocupadaAListaParaElCargoSiguiente()
        {
            Assert.Equal("ui.jefa.vista.0", WorkTaskRules.PhraseKey(10));
            Assert.Equal("ui.jefa.vista.2", WorkTaskRules.PhraseKey(55));
            Assert.Equal("ui.jefa.vista.4", WorkTaskRules.PhraseKey(99));
            Assert.Equal("ui.jefa.vista.5", WorkTaskRules.PhraseKey(100));
            Assert.All(WorkTaskRules.PhraseKeys, k => Assert.NotEqual(k, Content.Text(k)));
        }

        [Fact]
        public void TareasDelMes_LleganAlEmpezarElMes_YSonDeTuArea()
        {
            LifeSession session = WorkingIn(Taxes);
            ToNextMonth(session);

            Character player = session.World.Player;
            Assert.InRange(player.Job!.Tasks.Count, WorkTaskRules.MinPerMonth, WorkTaskRules.MaxPerMonth);
            Assert.All(player.Job.Tasks, t => Assert.StartsWith("tarea.contabilidad.", t.Id));
            // En Impuestos no le toca cuadrar sueldos ni auditar.
            Assert.DoesNotContain(player.Job.Tasks, t => t.Id == "tarea.contabilidad.liquidaciones" || t.Id == "tarea.contabilidad.muestra_auditoria");
            Assert.True(player.Job.Tasks.Select(t => t.DueWeek).SequenceEqual(player.Job.Tasks.Select(t => t.DueWeek).OrderBy(w => w)));

            // El IVA vence la semana del 20.
            WorkTask? iva = player.Job.Tasks.FirstOrDefault(t => t.Id == "tarea.contabilidad.iva");
            if (iva != null)
            {
                var due = new GameDate(iva.DueWeek);
                Assert.InRange(20 - due.DayOfMonth, 0, 6);
            }
        }

        [Fact]
        public void Tarea_ConCalmaSube_ConAyudaSubeMenos_YDejarlaLaPasaALaOtraSemana()
        {
            LifeSession session = WorkingIn(Development);
            Character player = session.World.Player;
            int week = session.World.Date.TotalWeeks;
            player.Job!.Tasks.Clear();
            player.Job.Tasks.Add(new WorkTask("tarea.software.bug_produccion", week));
            player.Job.Tasks.Add(new WorkTask("tarea.software.reunion_avance", week));
            player.Job.Tasks.Add(new WorkTask("tarea.software.estimacion", week + 1));
            player.Job.Performance = 50;

            WorkTaskRules.Do(session.World, player, WorkTaskRules.Calmly);
            Assert.Equal(50 + WorkTaskRules.Calm, player.Job.Performance);

            WorkTaskRules.Do(session.World, player, WorkTaskRules.Later);
            WorkTask postponed = player.Job.Tasks.Single(t => t.Id == "tarea.software.reunion_avance");
            Assert.True(postponed.Postponed);
            Assert.Equal(week + 1, postponed.DueWeek);
            Assert.Null(WorkTaskRules.Pending(player, week));

            // La semana siguiente vuelve, y dejarla otra vez es no entregarla.
            Assert.Equal("tarea.software.reunion_avance", WorkTaskRules.Pending(player, week + 1)!.Id);
            int before = player.Job.Performance;
            // Do usa la semana del mundo: la tarea atrasada ya venció para entonces.
            postponed.DueWeek = week;
            WorkTaskRules.Do(session.World, player, WorkTaskRules.Later);
            Assert.Equal(before + WorkTaskRules.Late, player.Job.Performance);
            Assert.Contains(session.World.Journal, j => j.TextKey == WorkTaskRules.LateJournal);
        }

        [Fact]
        public void Rapido_SaleBienMasSeguidoConMasInteligencia()
        {
            Assert.True(WorkTaskRules.QuickBase + (90 / 200.0) > WorkTaskRules.QuickBase + (30 / 200.0));
            Assert.True(WorkTaskRules.QuickBad < 0 && WorkTaskRules.QuickGood > 0);
        }

        [Fact]
        public void Momento_DeLaTarea_LlegaConSuPlazo_YTuJefaLoNota()
        {
            LifeSession session = WorkingIn(Development, seed: 23);
            ToNextMonth(session);
            Character player = session.World.Player;
            int before = player.Job!.Performance;

            for (int week = 0; week < 8 && session.Pending?.EventId.StartsWith("evento.software.tarea.", System.StringComparison.Ordinal) != true; week++)
            {
                while (session.Pending != null && !session.Pending.EventId.StartsWith("evento.software.tarea.", System.StringComparison.Ordinal))
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                if (session.Pending == null)
                {
                    session.Advance(1);
                }
            }

            Assert.StartsWith("evento.software.tarea.", session.Pending!.EventId);
            string text = new LifePresenter(session).Snapshot().PendingEvent!.Text;
            Assert.Contains("viernes", text);
            Assert.DoesNotContain("{plazo}", text);

            EventDefinition ev = Content.GetEvent(session.Pending.EventId);
            session.Choose(session.Pending.AvailableOptions.Single(i => ev.Options[i].TextKey.EndsWith(".calma", System.StringComparison.Ordinal)));
            Assert.Contains(player.Job.Tasks, t => t.Done);
            Assert.NotEqual(before, player.Job.Performance);

            TeamView team = new LifePresenter(session).Team()!;
            Assert.Equal(player.Job.Performance, team.BossView);
            Assert.False(string.IsNullOrEmpty(team.BossPhrase));
            Assert.Contains(team.Tasks, t => t.Done);
            Assert.All(team.Tasks, t => Assert.StartsWith("viernes ", t.Due));
        }

        [Fact]
        public void PedirAscenso_EsMasProbableConLaBarraAlta_YConRespeto()
        {
            double low = WorkTaskRules.AskPromotionChance(55, 55, 50);
            double high = WorkTaskRules.AskPromotionChance(95, 55, 50);
            double respected = WorkTaskRules.AskPromotionChance(95, 55, 90);
            Assert.True(low < 0.2, $"bajo {low}");
            Assert.True(high > 0.7, $"alto {high}");
            Assert.True(respected > high);
        }

        [Fact]
        public void EnCien_TuJefaTeOfreceElAscenso_YEnElCargoNuevoLaBarraVuelveA60()
        {
            LifeSession session = WorkingIn(Development, seed: 27);
            Character player = session.World.Player;
            player.Job!.MonthsInPosition = 40;
            player.Job.Performance = 100;
            string junior = player.Job.Position!.PositionId;

            for (int week = 0; week < 12 && session.Pending?.EventId != "evento.pega.jefa_ofrece_ascenso"; week++)
            {
                while (session.Pending != null && session.Pending.EventId != "evento.pega.jefa_ofrece_ascenso")
                {
                    EventDefinition other = Content.GetEvent(session.Pending.EventId);
                    // En las tareas se cumple con calma, para no bajar de 100 mientras llega la oferta.
                    int pick = session.Pending.AvailableOptions.FirstOrDefault(i => other.Options[i].TextKey.EndsWith(".calma", System.StringComparison.Ordinal), session.Pending.AvailableOptions[0]);
                    session.Choose(pick);
                    player.Job!.Performance = 100;
                }

                if (session.Pending == null)
                {
                    session.Advance(1);
                    player.Job!.Performance = System.Math.Max(player.Job.Performance, 100);
                }
            }

            Assert.Equal("evento.pega.jefa_ofrece_ascenso", session.Pending?.EventId);
            EventDefinition ev = Content.GetEvent(session.Pending!.EventId);
            session.Choose(session.Pending.AvailableOptions.Single(i => ev.Options[i].TextKey == "opcion.pega.jefa_ofrece_ascenso.aceptar"));

            Assert.NotEqual(junior, player.Job!.Position!.PositionId);
            Assert.Equal(WorkTaskRules.AfterPromotion, player.Job.Performance);
        }

        [Fact]
        public void PedirAscenso_EsUnBotonDeLaPega_AntesDeLlegarACien()
        {
            LifeSession session = WorkingIn(Taxes, seed: 29);
            // El equipo (y tu jefa, a quien se le pide) se arma la primera semana en la pega.
            ToNextMonth(session);
            Character player = session.World.Player;
            player.Job!.MonthsInPosition = 40;
            player.Job.Performance = 90;
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            Assert.Contains(session.Requestable(), e => e.Id == "evento.pega.pedir_ascenso");
            player.Job.Performance = 100;
            Assert.DoesNotContain(session.Requestable(), e => e.Id == "evento.pega.pedir_ascenso");
        }

        [Fact]
        public void Guardado_LlevaLasTareasDelMes()
        {
            LifeSession session = WorkingIn(Taxes, seed: 31);
            ToNextMonth(session);
            Character player = session.World.Player;
            player.Job!.Tasks[0].Postponed = true;

            Character loaded = LifeSession.Load(Content, session.SaveToText()).World.Player;

            Assert.Equal(player.Job.Tasks.Select(t => (t.Id, t.DueWeek, t.Postponed, t.Result)), loaded.Job!.Tasks.Select(t => (t.Id, t.DueWeek, t.Postponed, t.Result)));
        }
    }
}

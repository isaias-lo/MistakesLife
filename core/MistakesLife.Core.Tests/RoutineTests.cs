using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class RoutineTests
    {
        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        /// <summary>Una vida en Chile que ya tiene <paramref name="age"/> años (solo pasa el tiempo, sin sistemas).</summary>
        private static LifeSession Session(int age, ulong seed = 5)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            return session;
        }

        /// <summary>Corre solo el horario durante <paramref name="weeks"/> semanas.</summary>
        private static int RoutineWeeks(LifeSession session, int weeks, out bool stopped) =>
            new WeekCycle().Register(new RoutineSystem(session.Content) { StopsForNewRoutine = true }).AdvanceWeeks(session.World, weeks, out stopped);

        private static void FillWith(LifeSession session, string? activityId)
        {
            for (int day = 0; day < Routine.Days; day++)
            {
                foreach (RoutineBlock block in new[] { RoutineBlock.Afternoon, RoutineBlock.Night })
                {
                    session.World.Player.Routine!.Set(day, block, activityId);
                }
            }
        }

        [Fact]
        public void Horario_AntesDeLos6_NoExiste()
        {
            LifeSession session = Session(4);
            RoutineWeeks(session, 4, out _);
            Assert.Null(session.World.Player.Routine);
        }

        [Fact]
        public void Horario_A_Los6_SeSugiereYDetieneElTiempoParaArmarlo()
        {
            LifeSession session = Session(6);
            int weeks = RoutineWeeks(session, 10, out bool stopped);

            Routine routine = Assert.IsType<Routine>(session.World.Player.Routine);
            Assert.True(stopped);
            Assert.Equal(1, weeks);
            Assert.False(routine.Confirmed);
            Assert.Contains(routine.Slots, id => id != null);
        }

        [Fact]
        public void Horario_EnLaMismaEtapa_NoSeVuelveAPedir()
        {
            LifeSession session = Session(7);
            RoutineWeeks(session, 1, out _);
            session.ConfirmRoutine();

            int weeks = RoutineWeeks(session, 20, out bool stopped);

            Assert.False(stopped);
            Assert.Equal(20, weeks);
            Assert.True(session.World.Player.Routine!.Confirmed);
        }

        [Fact]
        public void Horario_AlCambiarDeEtapa_SeArmaOtro()
        {
            LifeSession session = Session(13);
            RoutineWeeks(session, 1, out _);
            session.ConfirmRoutine();
            string before = session.World.Player.Routine!.StageKey;

            RoutineWeeks(session, 6 * GameDate.WeeksPerYear, out bool stopped);

            Assert.True(stopped);
            Assert.NotEqual(before, session.World.Player.Routine!.StageKey);
            Assert.False(session.World.Player.Routine.Confirmed);
        }

        [Fact]
        public void Animo_ConUnHorarioAlegre_SeAcercaPocoAPocoSinPegarseEn100()
        {
            LifeSession session = Session(10);
            RoutineWeeks(session, 1, out _);
            FillWith(session, "actividad.tele");
            session.World.Player.Stats.Happiness = 100;

            RoutineWeeks(session, 40, out _);

            int target = RoutineRules.MoodTarget(RoutineRules.Total(session.World, session.Content, session.World.Player, session.World.Player.Routine!, "animo"));
            Assert.True(target < 100);
            Assert.Equal(target, session.World.Player.Stats.Happiness);
        }

        [Fact]
        public void Animo_SinNadaQueHacer_VuelveHacia50()
        {
            LifeSession session = Session(10);
            RoutineWeeks(session, 1, out _);
            FillWith(session, null);
            session.World.Player.Stats.Happiness = 0;

            RoutineWeeks(session, 30, out _);

            Assert.Equal(30, session.World.Player.Stats.Happiness);
        }

        [Fact]
        public void Horario_SinPantallaQueLoPida_NoDetieneElTiempo()
        {
            var session = new LifeSession(RepoPacks.Content, 5, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, 6 * GameDate.WeeksPerYear);

            int weeks = new WeekCycle().Register(new RoutineSystem(session.Content)).AdvanceWeeks(session.World, 10, out bool stopped);

            Assert.False(stopped);
            Assert.Equal(10, weeks);
            Assert.NotNull(session.World.Player.Routine);
        }

        [Fact]
        public void Mente_EstudiandoTodasLasTardes_SubeUnPuntoCadaMes()
        {
            LifeSession session = Session(10);
            RoutineWeeks(session, 1, out _);
            FillWith(session, "actividad.estudiar");
            session.World.Player.Stats.Smarts = 40;

            RoutineWeeks(session, GameDate.WeeksPerYear, out _);

            Assert.InRange(session.World.Player.Stats.Smarts, 51, 53);
        }

        [Fact]
        public void Carino_IrALaPlazaConUnAmigo_LosAcerca()
        {
            LifeSession session = Session(11);
            Run(session, "jugador.conocer('amistad')");
            Character friend = RelationshipRules.WithBond(session.World, session.World.Player, RelationshipBond.Friend).OrderBy(c => c.Id).Last();
            RoutineWeeks(session, 1, out _);
            FillWith(session, null);
            session.World.Player.Routine!.Set(1, RoutineBlock.Afternoon, "actividad.plaza_amigos");
            session.World.Player.Routine!.Set(4, RoutineBlock.Afternoon, "actividad.plaza_amigos");
            int before = RelationshipRules.Feelings(session.World, friend, session.World.Player).Affection;

            RoutineWeeks(session, 26, out _);

            Assert.True(RelationshipRules.Feelings(session.World, friend, session.World.Player).Affection > before);
        }

        [Fact]
        public void SetRoutine_ConAlgoQueNoSePuede_Falla()
        {
            LifeSession session = Session(7);
            RoutineWeeks(session, 1, out _);

            Assert.Throws<InvalidOperationException>(() => session.SetRoutine(0, RoutineBlock.Night, "actividad.carrete"));
            Assert.Throws<InvalidOperationException>(() => session.SetRoutine(0, RoutineBlock.Night, "actividad.deporte"));
            session.SetRoutine(0, RoutineBlock.Afternoon, "actividad.deporte");
            Assert.Equal("actividad.deporte", session.World.Player.Routine!.Get(0, RoutineBlock.Afternoon));
        }

        [Fact]
        public void Horario_SeGuardaYSeCarga()
        {
            LifeSession session = Session(8);
            RoutineWeeks(session, 1, out _);
            session.SetRoutine(3, RoutineBlock.Afternoon, "actividad.leer");
            session.ConfirmRoutine();

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Routine routine = loaded.World.Player.Routine!;
            Assert.True(routine.Confirmed);
            Assert.Equal(session.World.Player.Routine!.StageKey, routine.StageKey);
            Assert.Equal(session.World.Player.Routine.Slots, routine.Slots);
        }

        [Fact]
        public void Presentador_MuestraElHorarioConTextosYHaciaDondeVa()
        {
            LifeSession session = Session(9);
            RoutineWeeks(session, 1, out _);
            FillWith(session, "actividad.estudiar");
            var presenter = new LifePresenter(session);

            RoutineView view = presenter.Routine()!;

            Assert.Equal(14, view.Slots.Count);
            Assert.All(view.Slots, s => Assert.Equal("estudiar", s.Text));
            Assert.Equal("sube", view.Mind);
            Assert.Contains(presenter.RoutineOptions(RoutineBlock.Afternoon), o => o.ActivityId == "actividad.deporte");
            Assert.DoesNotContain(presenter.RoutineOptions(RoutineBlock.Night), o => o.ActivityId == "actividad.deporte");
        }
    }
}

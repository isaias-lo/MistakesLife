using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Hacer cosas con el tiempo parado, con las tardes libres del mes (D3 en docs/roadmap.md).</summary>
    public class FreeTimeActionsTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        /// <summary>Una vida de esa edad con un horario lleno de deporte salvo <paramref name="free"/> noches de tele.</summary>
        private static LifeSession Life(int age, int free)
        {
            var session = new LifeSession(Content, 5, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            var routine = new Routine("libre");
            for (int day = 0; day < Routine.Days; day++)
            {
                routine.Set(day, RoutineBlock.Afternoon, "actividad.deporte");
                routine.Set(day, RoutineBlock.Night, day < free ? "actividad.tele" : "actividad.dormir_temprano");
            }

            session.World.Player.Routine = routine;
            session.World.Player.Stats.Energy = 50;
            return session;
        }

        private static Character Player(LifeSession session) => session.World.Player;

        private static Character NewFriend(LifeSession session)
        {
            Run(session, "jugador.conocer('amistad')");
            return RelationshipRules.WithBond(session.World, Player(session), RelationshipBond.Friend).OrderBy(c => c.Id).Last();
        }

        [Fact]
        public void TardesLibres_SalenDelHorario_LoVacioYLaTeleCuentan()
        {
            LifeSession session = Life(20, free: 3);
            Assert.Equal(3, FreeTimeRules.Total(Content, Player(session)));

            Player(session).Routine!.Set(6, RoutineBlock.Afternoon, null);
            Assert.Equal(4, FreeTimeRules.Total(Content, Player(session)));

            Player(session).Routine = new Routine("libre");
            Assert.Equal(FreeTimeRules.MaxPerMonth, FreeTimeRules.Total(Content, Player(session)));

            Player(session).Routine = null;
            Assert.Equal(0, FreeTimeRules.Total(Content, Player(session)));
        }

        [Fact]
        public void Descansar_GastaUnaTarde_YLaEnergiaSubeAlTiro()
        {
            LifeSession session = Life(20, free: 3);
            var changes = session.DoAction("accion.descansar", null).Changes;

            Assert.Equal(56, Player(session).Stats.Energy);
            Assert.Equal("energia", Assert.Single(changes).Key);
            Assert.Equal(2, FreeTimeRules.Left(session.World, Content, Player(session)));
        }

        [Fact]
        public void SinTardesLibres_SeVe_PeroNoSePuede()
        {
            LifeSession session = Life(20, free: 0);
            var life = new LifePresenter(session);

            DoableActionView rest = life.Actions("salud").First(a => a.Id == "accion.descansar");
            Assert.False(rest.CanAfford);
            Assert.Throws<System.InvalidOperationException>(() => session.DoAction("accion.descansar", null));
            Assert.Equal(50, Player(session).Stats.Energy);
        }

        [Fact]
        public void CadaAccion_TieneSusVecesAlMes_YElMesQueVieneVuelve()
        {
            LifeSession session = Life(20, free: 7);
            session.DoAction("accion.descansar", null);
            session.DoAction("accion.descansar", null);
            Assert.DoesNotContain(new LifePresenter(session).Actions("salud"), a => a.Id == "accion.descansar");

            new WeekCycle().AdvanceWeeks(session.World, session.World.Date.WeeksToNextMonth);
            Assert.Contains(new LifePresenter(session).Actions("salud"), a => a.Id == "accion.descansar");
            Assert.Equal(7, FreeTimeRules.Left(session.World, Content, Player(session)));
        }

        [Fact]
        public void JuntarseConUnAmigo_ElCarinoSubeDeLosDosLados_UnaVezAlMesConCadaUno()
        {
            LifeSession session = Life(20, free: 4);
            Character friend = NewFriend(session);
            int theirs = RelationshipRules.Feelings(session.World, friend, Player(session)).Affection;
            int mine = RelationshipRules.Feelings(session.World, Player(session), friend).Affection;
            int happiness = Player(session).Stats.Happiness;

            ActionResultView result = new LifePresenter(session).DoAction("accion.juntarse_amigo", friend.Id);

            Assert.Equal(theirs + 3, RelationshipRules.Feelings(session.World, friend, Player(session)).Affection);
            Assert.Equal(mine + 3, RelationshipRules.Feelings(session.World, Player(session), friend).Affection);
            Assert.True(Player(session).Stats.Happiness > happiness);
            Assert.Contains(friend.FirstName, result.Title);
            Assert.Contains(friend.FirstName, result.Text);
            Assert.Contains(result.Changes, c => c.Label.Contains(friend.FirstName) && c.IsGood);
            Assert.Contains(result.Changes, c => c.Label.StartsWith("Energía", System.StringComparison.Ordinal) && !c.IsGood);
            Assert.Equal(3, result.FreeTime.Left);
            Assert.DoesNotContain(new LifePresenter(session).Actions("gente"), a => a.PersonId == friend.Id && a.Id == "accion.juntarse_amigo");
        }

        [Fact]
        public void EstudiarParaLaPrueba_EnElColegioYaNoSale_PorqueTodosLosRamosTienenPreguntas()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            var decider = new AutoDecider(Content);
            var random = new GameRandom(3);
            while (Player(session).AgeInYears(session.World.Date) < 10 || Player(session).SchoolRecord?.TestPending != true)
            {
                Assert.True(Player(session).AgeInYears(session.World.Date) < 14, "no se anunció ninguna prueba entre los 10 y los 14 años.");
                if (session.Pending != null)
                {
                    session.Choose(decider.Choose(session.World, session.Pending, random));
                }
                else
                {
                    session.Advance(1);
                }
            }

            if (session.Pending != null)
            {
                session.Choose(decider.Choose(session.World, session.Pending, random));
            }

            // Desde D5a2 todos los ramos con prueba escrita del colegio tienen preguntas: la prueba
            // se prepara estudiando el ramo (ver StudyTests.EstudiarElRamoDeLaPrueba_LaDejaMasPreparada).
            Player(session).Routine = new Routine("libre");
            var life = new LifePresenter(session);
            Assert.DoesNotContain(life.Actions("estudios"), a => a.Id == "accion.estudiar_prueba");

            Assert.DoesNotContain(new LifePresenter(Life(20, free: 3)).Actions("estudios"), a => a.Id == "accion.estudiar_prueba");
        }

        [Fact]
        public void Contador_DiceCuantasQuedanYDeCuantas()
        {
            LifeSession session = Life(13, free: 5);
            session.DoAction("accion.deporte", null);
            FreeTimeView view = new LifePresenter(session).FreeTime()!;

            Assert.Equal(4, view.Left);
            Assert.Equal(5, view.Total);
            Assert.Contains("4 tardes libres en", view.Label);
            Assert.Equal("tu horario deja 5 este mes", view.Note);
        }

        [Fact]
        public void ConUnEventoEsperando_PrimeroSeDecide()
        {
            LifeSession session = Life(20, free: 3);
            var life = new LifePresenter(session);
            while (session.Pending == null)
            {
                session.Advance(4);
            }

            Assert.Empty(life.Actions("salud"));
            Assert.Throws<System.InvalidOperationException>(() => session.DoAction("accion.descansar", null));
        }

        [Fact]
        public void Guardado_RecuerdaLasTardesUsadasDelMes()
        {
            LifeSession session = Life(20, free: 3);
            session.DoAction("accion.descansar", null);

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());
            Assert.Equal(2, FreeTimeRules.Left(loaded.World, Content, loaded.World.Player));
            Assert.Equal(new[] { "accion.descansar" }, loaded.World.Player.FreeTime!.Done.ToArray());
        }

        [Fact]
        public void AccionMalArmada_SeDetecta()
        {
            var content = new ContentDatabase();
            content.AddActionsJson("pack.prueba", "acciones.json", @"[
              { ""id"": ""accion.a"", ""grupo"": ""cocina"", ""cambia"": { ""inteligencia"": 1 } },
              { ""id"": ""accion.b"", ""grupo"": ""salud"", ""cambia"": { ""carino"": 2 } },
              { ""id"": ""accion.c"", ""grupo"": ""salud"", ""cambia"": {} },
              { ""id"": ""accion.d"", ""grupo"": ""estudios"", ""cambia"": { ""preparar_prueba"": 1 } },
              { ""id"": ""hacer.e"", ""grupo"": ""salud"", ""cambia"": { ""suerte"": 2 } }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Source.Contains("accion.a") && e.Message.Contains("grupo"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("accion.b") && e.Message.Contains("con quién"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("accion.c") && e.Message.Contains("cambiar algo"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("accion.d") && e.Message.Contains("prueba_anunciada"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("hacer.e") && e.Message.Contains("suerte"));
        }
    }
}

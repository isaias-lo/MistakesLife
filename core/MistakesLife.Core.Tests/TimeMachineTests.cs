using System;
using System.Linq;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class TimeMachineTests
    {
        /// <summary>
        /// Juega hasta esa edad del jugador, decidiendo solo según el estado del
        /// mundo (así dos partidas iguales deciden igual). Devuelve false si el
        /// jugador murió antes.
        /// </summary>
        private static bool PlayUntilAge(LifeSession session, int age)
        {
            var decider = new AutoDecider(session.Content);
            int target = age * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive)
            {
                PendingEvent? pending = session.Pending;
                if (pending != null)
                {
                    session.Choose(Decide(session, decider, pending));
                    continue;
                }

                int weeks = target - session.World.Player.AgeInWeeks(session.World.Date);
                if (weeks <= 0)
                {
                    return true;
                }

                session.Advance(weeks);
            }

            return false;
        }

        private static int Decide(LifeSession session, AutoDecider decider, PendingEvent pending)
        {
            var chooser = new GameRandom(session.World.Seed).Fork("prueba." + session.World.Date.TotalWeeks + "." + session.World.EventHistory.Count);
            return decider.Choose(session.World, pending, chooser);
        }

        /// <summary>Juega hasta que el jugador tenga que responder un evento de día clave.</summary>
        private static LifeSession PlayUntilKeyDay(out PendingEvent keyDay)
        {
            for (ulong seed = 1; seed < 40; seed++)
            {
                var session = new LifeSession(RepoPacks.Content, seed);
                var decider = new AutoDecider(session.Content);
                while (session.World.Player.IsAlive && session.PlayerAgeInYears < 60)
                {
                    PendingEvent? pending = session.Pending;
                    if (pending == null)
                    {
                        session.Advance(GameDate.WeeksPerYear);
                        continue;
                    }

                    if (session.Content.GetEvent(pending.EventId).KeyDay && pending.AvailableOptions.Count > 1)
                    {
                        keyDay = pending;
                        return session;
                    }

                    session.Choose(Decide(session, decider, pending));
                }
            }

            throw new InvalidOperationException("Ninguna vida de prueba tuvo un día clave con opciones.");
        }

        [Theory]
        [InlineData(0, LifeStages.Infancy)]
        [InlineData(5, LifeStages.Infancy)]
        [InlineData(6, LifeStages.Childhood)]
        [InlineData(17, LifeStages.Adolescence)]
        [InlineData(18, LifeStages.Youth)]
        [InlineData(30, LifeStages.Adulthood)]
        [InlineData(45, LifeStages.Maturity)]
        [InlineData(64, LifeStages.Maturity)]
        [InlineData(65, LifeStages.OldAge)]
        [InlineData(100, LifeStages.OldAge)]
        public void EtapaDeLaVida_SegunLaEdad(int age, string stage)
        {
            Assert.Equal(stage, LifeStages.StageAt(age));
        }

        [Fact]
        public void EtapaDeLaVida_SabeCuantoFaltaParaLaSiguiente()
        {
            Assert.Equal(6 * GameDate.WeeksPerYear, LifeStages.WeeksToNextStage(0));
            Assert.Equal(1, LifeStages.WeeksToNextStage((18 * GameDate.WeeksPerYear) - 1));
            Assert.Equal(12 * GameDate.WeeksPerYear, LifeStages.WeeksToNextStage(18 * GameDate.WeeksPerYear));
            Assert.Null(LifeStages.WeeksToNextStage(70 * GameDate.WeeksPerYear));
        }

        [Fact]
        public void PartidaNueva_EmpiezaConUnaCarga_YSacaUnaFotoAlEmpezarCadaEtapa()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            Assert.Equal(TimeMachine.StartingCharges, session.TimeMachine.Charges);
            Assert.True(PlayUntilAge(session, 20));

            var stages = session.TimeMachine.Snapshots.Where(s => s.Kind == SnapshotKind.StageStart).ToList();
            Assert.Equal(new[] { LifeStages.Infancy, LifeStages.Childhood, LifeStages.Adolescence, LifeStages.Youth }, stages.Select(s => s.StageId));
            Assert.All(stages, s => Assert.Equal(LifeStages.StartAge(s.StageId), s.PlayerAgeInYears));

            // La foto es del primer día de la etapa, aunque el tiempo corriera de a varios años.
            WorldSnapshot youth = stages.Last();
            Assert.Equal(18 * GameDate.WeeksPerYear, session.World.Player.AgeInWeeks(youth.Date));
        }

        [Fact]
        public void VolverAlInicioDeUnaEtapa_DevuelveElMundoDeEseDia_YDejaUnaIntuicion()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            Assert.True(PlayUntilAge(session, 25));
            int playerId = session.World.PlayerId;
            WorldSnapshot youth = session.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Youth && s.Kind == SnapshotKind.StageStart);
            Assert.True(session.TimeMachine.CanGoBackTo(youth));

            session.GoBack(youth.Id);

            Assert.Equal(youth.Date, session.World.Date);
            Assert.Equal(playerId, session.World.PlayerId);
            Assert.Equal(18, session.PlayerAgeInYears);
            Assert.Equal(TimeMachine.StartingCharges - 1, session.TimeMachine.Charges);
            Assert.Contains(LifeStages.Youth, session.TimeMachine.UsedStages);
            Assert.DoesNotContain(session.TimeMachine.Snapshots, s => s.Date > youth.Date);
            Assert.Equal(LifeSession.JournalIntuition, session.World.Journal.Last().TextKey);
            Assert.Contains(session.TakeNewJournalEntries(), e => e.TextKey == LifeSession.JournalIntuition);
        }

        [Fact]
        public void VolverAlPasado_YDecidirIgual_LaVidaSigueIgualQueLaPrimeraVez()
        {
            var first = new LifeSession(RepoPacks.Content, 11);
            Assert.True(PlayUntilAge(first, 30));

            var second = new LifeSession(RepoPacks.Content, 11);
            Assert.True(PlayUntilAge(second, 40));
            second.GoBack(second.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Adulthood).Id);
            Assert.True(PlayUntilAge(second, 30));

            Assert.Equal(first.World.Date, second.World.Date);
            Assert.Equal(first.World.Player.Money, second.World.Player.Money);
            Assert.Equal(first.World.Characters.Count, second.World.Characters.Count);
            Assert.Equal(first.World.EventHistory.Select(r => r.EventId), second.World.EventHistory.Select(r => r.EventId));
        }

        [Fact]
        public void VolverAntesDeUnDiaClave_ElEventoEspera_YSeRecuerdaLoQueSeEligio()
        {
            LifeSession session = PlayUntilKeyDay(out PendingEvent keyDay);
            WorldSnapshot before = Assert.Single(session.TimeMachine.Snapshots, s => s.Kind == SnapshotKind.BeforeDecision);
            Assert.Equal(keyDay.EventId, before.EventId);
            Assert.Null(session.TimeMachine.PreviousChoice(keyDay));

            int chosen = keyDay.AvailableOptions.Last();
            session.Choose(chosen);
            session.Advance(GameDate.WeeksPerYear);

            session.GoBack(before.Id);

            PendingEvent again = Assert.IsType<PendingEvent>(session.Pending);
            Assert.Equal(keyDay.EventId, again.EventId);
            Assert.Equal(keyDay.Date, again.Date);
            Assert.Equal(keyDay.AvailableOptions, again.AvailableOptions);
            Assert.Equal(chosen, session.TimeMachine.PreviousChoice(again));
            Assert.Equal(LifeSession.JournalIntuitionDecision, session.World.Journal.Last().TextKey);

            // Volver no saca una foto repetida del mismo día clave.
            session.Choose(again.AvailableOptions.First());
            Assert.Single(session.TimeMachine.Snapshots, s => s.Kind == SnapshotKind.BeforeDecision && s.EventId == keyDay.EventId);
        }

        [Fact]
        public void SinCargas_NoSePuedeVolver_HastaQueSeSumen()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            Assert.True(PlayUntilAge(session, 20));
            session.GoBack(session.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Adolescence).Id);
            Assert.Equal(0, session.TimeMachine.Charges);

            WorldSnapshot childhood = session.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Childhood);
            Assert.False(session.TimeMachine.CanGoBackTo(childhood));
            Assert.Throws<InvalidOperationException>(() => session.GoBack(childhood.Id));

            session.TimeMachine.AddCharges(2);
            Assert.True(session.TimeMachine.CanGoBackTo(childhood));
            session.GoBack(childhood.Id);
            Assert.Equal(6, session.PlayerAgeInYears);
            Assert.Equal(1, session.TimeMachine.Charges);
        }

        [Fact]
        public void CadaEtapa_SeVuelveAVivirUnaSolaVez()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            session.TimeMachine.AddCharges(5);
            Assert.True(PlayUntilAge(session, 25));
            session.GoBack(session.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Youth).Id);
            Assert.True(PlayUntilAge(session, 25));

            WorldSnapshot youth = session.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Youth);
            Assert.False(session.TimeMachine.CanGoBackTo(youth));
            Assert.Throws<InvalidOperationException>(() => session.GoBack(youth.Id));

            // Otra etapa sí se puede volver a vivir.
            Assert.True(session.TimeMachine.CanGoBackTo(session.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Adolescence)));
        }

        [Fact]
        public void Pantalla_ConCargaYFotos_MuestraMomentosConSuEdadYSuEtapa()
        {
            var session = new LifeSession(RepoPacks.Content, 11);
            Assert.True(PlayUntilAge(session, 20));
            var presenter = new LifePresenter(session);

            TimeTravelView travel = presenter.TimeTravel();
            Assert.True(travel.CanGoBack);
            Assert.Equal(TimeMachine.StartingCharges, travel.Charges);
            Assert.Contains("Te quedan 1 cargas", travel.ChargesLabel);
            Assert.All(travel.Moments, m => Assert.StartsWith($"A tus {m.AgeInYears} años, ", m.Label));
        }

        [Fact]
        public void Pantalla_VolverGastaLaCarga_YSinCargasNoHayMomentos()
        {
            var session = new LifeSession(RepoPacks.Content, 11);
            Assert.True(PlayUntilAge(session, 20));
            var presenter = new LifePresenter(session);
            int first = presenter.TimeTravel().Moments[0].AgeInYears;

            presenter.GoBack(0);

            Assert.True(session.PlayerAgeInYears <= first);
            TimeTravelView after = presenter.TimeTravel();
            Assert.Equal(0, after.Charges);
            Assert.False(after.CanGoBack);
            Assert.Throws<ArgumentOutOfRangeException>(() => presenter.GoBack(0));
        }

        [Fact]
        public void Pantalla_AlVolverAntesDeDecidir_MarcaLaOpcionDeLaOtraVez()
        {
            LifeSession session = PlayUntilKeyDay(out PendingEvent keyDay);
            var presenter = new LifePresenter(session);
            Assert.Null(presenter.Snapshot().PendingEvent!.IntuitionOption);

            int position = keyDay.AvailableOptions.Count - 1;
            presenter.Choose(position);
            session.Advance(GameDate.WeeksPerYear);

            TimeTravelView travel = presenter.TimeTravel();
            WorldSnapshot before = session.TimeMachine.Snapshots.Single(s => s.Kind == SnapshotKind.BeforeDecision);
            int moment = travel.Moments.ToList().FindIndex(m => m.SnapshotId == before.Id);
            Assert.Contains("antes de decidir", travel.Moments[moment].Label);
            presenter.GoBack(moment);

            Assert.Equal(position, presenter.Snapshot().PendingEvent!.IntuitionOption);
        }

        [Fact]
        public void SoloSeGuardanLasUltimasFotosDeAntesDeDecidir()
        {
            var session = new LifeSession(RepoPacks.Content, 5);
            PlayUntilAge(session, 70);
            Assert.InRange(session.TimeMachine.Snapshots.Count(s => s.Kind == SnapshotKind.BeforeDecision), 0, TimeMachine.MaxDecisionSnapshots);
        }

        [Fact]
        public void GuardarYCargar_ConservaLasFotosLasCargasYLasIntuiciones()
        {
            LifeSession session = PlayUntilKeyDay(out PendingEvent keyDay);
            session.TimeMachine.AddCharges(3);
            WorldSnapshot before = session.TimeMachine.Snapshots.Single(s => s.Kind == SnapshotKind.BeforeDecision);
            session.Choose(keyDay.AvailableOptions.Last());
            session.GoBack(before.Id);

            string saved = session.SaveToText();
            LifeSession loaded = LifeSession.Load(RepoPacks.Content, saved);

            Assert.Equal(saved, loaded.SaveToText());
            Assert.Equal(session.TimeMachine.Charges, loaded.TimeMachine.Charges);
            Assert.Equal(session.TimeMachine.UsedStages, loaded.TimeMachine.UsedStages);
            Assert.Equal(session.TimeMachine.Snapshots.Select(s => (s.Id, s.Kind, s.Date, s.StageId, s.EventId)), loaded.TimeMachine.Snapshots.Select(s => (s.Id, s.Kind, s.Date, s.StageId, s.EventId)));
            Assert.Equal(keyDay.AvailableOptions.Last(), loaded.TimeMachine.PreviousChoice(loaded.Pending!));

            // Una foto cargada sirve para volver igual que antes de guardar.
            WorldSnapshot birth = loaded.TimeMachine.Snapshots.First();
            loaded.GoBack(birth.Id);
            Assert.Equal(0, loaded.World.Player.AgeInWeeks(loaded.World.Date));
            Assert.True(PlayUntilAge(loaded, 2));
        }

        [Fact]
        public void Cargar_UnGuardadoDeLaVersion4_EmpiezaConUnaCargaYSacaSuPrimeraFoto()
        {
            var session = new LifeSession(RepoPacks.Content, 6);
            Assert.True(PlayUntilAge(session, 20));
            var json = Newtonsoft.Json.Linq.JObject.Parse(session.SaveToText());
            json.Remove("timeMachine");
            json["version"] = 4;

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, json.ToString());

            Assert.Equal(TimeMachine.StartingCharges, loaded.TimeMachine.Charges);
            Assert.Empty(loaded.TimeMachine.Snapshots);
            loaded.Advance(1);
            WorldSnapshot first = Assert.Single(loaded.TimeMachine.Snapshots);
            Assert.Equal(LifeStages.Youth, first.StageId);
        }

        [Fact]
        public void Legado_LaNuevaGeneracionEmpiezaSinFotosViejas_YConservaLasCargas()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            Assert.True(PlayUntilAge(session, 20));
            session.TimeMachine.AddCharges(2);
            session.GoBack(session.TimeMachine.Snapshots.First(s => s.StageId == LifeStages.Youth).Id);
            Life.FamilyFactory.CreateChild(session.World, session.Content, session.World.Player);
            session.World.Player.Die(session.World.Date);

            session.ContinueWith(session.Heirs[0].Id);

            Assert.Equal(2, session.TimeMachine.Charges);
            Assert.Empty(session.TimeMachine.UsedStages);
            Assert.All(session.TimeMachine.Snapshots, s => Assert.Equal(session.World.PlayerId, s.PlayerId));
            Assert.Single(session.TimeMachine.Snapshots);
        }

        [Fact]
        public void ConFotos_ElGuardadoDeUnaVidaLargaSigueSiendoLiviano()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            PlayUntilAge(session, 85);
            int bytes = session.SaveToBytes().Length;
            Assert.True(bytes < 80_000, $"El guardado pesa {bytes} bytes con {session.TimeMachine.Snapshots.Count} fotos.");
        }

        [Fact]
        public void MaquinaApagada_NoSacaFotos()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            session.TimeMachine.Enabled = false;
            PlayUntilAge(session, 20);
            Assert.Empty(session.TimeMachine.Snapshots);
        }

        [Fact]
        public void TextosDeVolverAlPasado_ExistenEnEspanol()
        {
            foreach (string key in LifeStages.All.Concat(new[] { LifeSession.JournalIntuition, LifeSession.JournalIntuitionDecision }))
            {
                Assert.NotEqual(key, RepoPacks.Content.Text(key));
            }
        }
    }
}

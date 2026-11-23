using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5c3: cuando un momento detiene el tiempo, la escena va al lugar donde pasa, con esa persona.</summary>
    public class MomentSceneTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Lugares de momentos que todavía no tienen escena propia: la escena se queda donde estaba.</summary>
        private static readonly string[] WithoutScene = { "lugar.dinero", "lugar.casino", "lugar.local_paes", "lugar.negocio" };

        [Fact]
        public void TodoLugarDeMomento_TieneSuLugarEnElBarrio_OEstaEnLaListaSinEscena()
        {
            var missing = Content.Events
                .Where(e => e.Place != null && !WithoutScene.Contains(e.Place) && Content.MapPlaceOfMoment(e.Place) == null)
                .Select(e => e.Id + " (" + e.Place + ")")
                .ToList();

            Assert.True(missing.Count == 0, "Momentos con un lugar que la escena no sabe mostrar: " + string.Join(", ", missing));
        }

        [Fact]
        public void LugaresDeMomentos_VanAlLugarDelBarrioQueCorresponde()
        {
            Assert.Equal("lugar.barrio.casa", Content.MapPlaceOfMoment("lugar.casa")!.Id);
            Assert.Equal("lugar.barrio.estudios", Content.MapPlaceOfMoment("lugar.sala")!.Id);
            Assert.Equal("lugar.barrio.estudios", Content.MapPlaceOfMoment("lugar.sala_u")!.Id);
            Assert.Equal("lugar.barrio.pega", Content.MapPlaceOfMoment("lugar.trabajo")!.Id);
            Assert.Equal("lugar.barrio.plaza", Content.MapPlaceOfMoment("lugar.plaza")!.Id);
            Assert.Equal("lugar.chile.fonda", Content.MapPlaceOfMoment("lugar.chile.fonda")!.Id);
            Assert.Null(Content.MapPlaceOfMoment("lugar.dinero"));
        }

        [Fact]
        public void Momento_LaEscenaVaAlLugarDondePasa_ConEsaPersona()
        {
            var session = new LifeSession(Content, 11, "pais.chile");
            session.TimeMachine.Enabled = false;
            var presenter = new LifePresenter(session);
            var seen = new HashSet<string>();
            int target = 30 * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target && seen.Count < 3)
            {
                PendingEvent? pending = session.Pending;
                if (pending == null)
                {
                    session.Advance(4);
                    continue;
                }

                EventDefinition definition = Content.GetEvent(pending.EventId);
                MomentSceneView? scene = presenter.MomentScene();
                MapPlaceDefinition? place = definition.Place == null ? null : Content.MapPlaceOfMoment(definition.Place);
                if (place == null)
                {
                    Assert.Null(scene);
                }
                else if (scene != null)
                {
                    seen.Add(scene.Scene);
                    Assert.StartsWith(presenter.Snapshot().PendingEvent!.Place!, scene.Caption);
                    var person = EventSystem.PersonOf(session.World, pending);
                    if (person != null && person.Id != session.World.PlayerId)
                    {
                        Assert.Equal(person.Id, scene.PersonId);
                        Assert.Contains(person.FirstName, scene.Caption);
                    }

                    switch (place.Open)
                    {
                        case MapPlaceDefinition.OpensHome:
                            Assert.Equal(MomentSceneView.Home, scene.Scene);
                            break;
                        case MapPlaceDefinition.OpensStudies:
                        case MapPlaceDefinition.OpensWork:
                            Assert.Equal(MomentSceneView.Section, scene.Scene);
                            Assert.NotNull(scene.Place);
                            break;
                        default:
                            Assert.Equal(MomentSceneView.Out, scene.Scene);
                            Assert.Equal(place.Shape, scene.Outing!.Shape);
                            Assert.Equal(scene.PersonId, scene.Outing.Companion?.Id);
                            break;
                    }
                }

                session.Choose(pending.AvailableOptions[0]);
            }

            Assert.Contains(MomentSceneView.Home, seen);
            Assert.Contains(MomentSceneView.Section, seen);
        }

        [Fact]
        public void SinMomento_LaEscenaNoCambia()
        {
            var session = new LifeSession(Content, 4, "pais.chile");
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            Assert.Null(new LifePresenter(session).MomentScene());
        }
    }
}

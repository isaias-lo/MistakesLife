using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class GriefTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static EventScope Scope(World world, Character? person, bool readOnly = true) =>
            new EventScope(world, Content, world.Player, null, readOnly, person);

        /// <summary>Muere <paramref name="dead"/> y queda el duelo del jugador, como lo hace la mortalidad.</summary>
        private static Grief? Dies(World world, Character dead)
        {
            dead.Die(world.Date);
            return GriefRules.Register(world, world.Player, dead, world.Date, world.Player.PartnerId);
        }

        private static Character Friend(World world, int affection)
        {
            Character friend = FamilyFactory.CreateAcquaintance(world, Content, world.Player, RelationshipBond.Friend);
            world.Player.RelationshipWith(friend.Id)!.Affection = affection;
            return friend;
        }

        [Fact]
        public void MuereLaMama_EsUnDueloPesadoQueBajaElTechoDeLaFelicidadYLaEnergia()
        {
            var session = new LifeSession(Content, 1);
            World world = session.World;
            Character mother = world.GetCharacter(world.Player.MotherId!.Value);
            world.Player.Stats.Happiness = 100;
            world.Player.Stats.Energy = 100;

            Grief grief = Dies(world, mother)!;
            new WeekCycle().Register(new GriefSystem()).AdvanceWeeks(world, 1);

            Assert.Equal("madre", grief.Relation);
            Assert.True(grief.Weight >= 60);
            int pain = GriefRules.CurrentPain(world.Player, world.Date);
            Assert.True(pain > 50);
            Assert.True(world.Player.Stats.Happiness <= 100 - (int)(pain * GriefRules.HappinessPerPain));
            Assert.True(world.Player.Stats.Energy < 100);
            Assert.True(Expression.Parse("jugador.en_duelo").Evaluate(Scope(world, null)).AsBool("prueba"));
        }

        [Fact]
        public void LaPena_BajaConElTiempoHastaPasar()
        {
            var session = new LifeSession(Content, 2);
            World world = session.World;
            Grief grief = Dies(world, world.GetCharacter(world.Player.MotherId!.Value))!;
            int duration = GriefRules.DurationWeeks(grief.Weight);

            Assert.Equal(grief.Weight, GriefRules.Pain(grief, grief.Since));
            Assert.True(GriefRules.Pain(grief, grief.Since.AddWeeks(duration / 2)) < grief.Weight);
            Assert.Equal(0, GriefRules.Pain(grief, grief.Since.AddWeeks(duration)));
            Assert.True(duration <= GriefRules.LongestWeeks);
        }

        [Fact]
        public void AmistadQueridaDueleMasQueUnConocido_YAlguienQueCasiNoConocias_NoEsDuelo()
        {
            var session = new LifeSession(Content, 3);
            World world = session.World;
            Character close = Friend(world, 90);
            Character distant = Friend(world, 5);

            Grief closeGrief = Dies(world, close)!;
            Grief? distantGrief = Dies(world, distant);

            Assert.Equal("amistad", closeGrief.Relation);
            Assert.True(closeGrief.Weight >= 60);
            Assert.Null(distantGrief);
        }

        [Fact]
        public void AcompanarseAcortaElDuelo_YEncerrarseLoAlarga()
        {
            var session = new LifeSession(Content, 4);
            World world = session.World;
            Character mother = world.GetCharacter(world.Player.MotherId!.Value);
            Grief grief = Dies(world, mother)!;
            int before = GriefRules.Pain(grief, grief.Since.AddWeeks(10));

            Effect.Parse("persona.aliviar_duelo(8)").Apply(Scope(world, mother, readOnly: false));

            Assert.Equal(8, grief.ReliefWeeks);
            Assert.True(GriefRules.Pain(grief, grief.Since.AddWeeks(10)) < before);
        }

        [Fact]
        public void LosMomentosDelDuelo_VenAQuienMurioConQuienEraYCuandoMurio()
        {
            var session = new LifeSession(Content, 5);
            World world = session.World;
            Character mother = world.GetCharacter(world.Player.MotherId!.Value);
            Dies(world, mother);

            EventScope scope = Scope(world, mother);
            Assert.Equal("madre", Expression.Parse("persona.lazo").Evaluate(scope).AsText("prueba"));
            Assert.Equal(0, Expression.Parse("persona.semanas_desde_muerte").Evaluate(scope).AsNumber("prueba"));
            Assert.True(Expression.Parse("persona.peso_duelo").Evaluate(scope).AsNumber("prueba") >= 60);
            Assert.Contains(GriefRules.Departed(world, world.Player), c => c.Id == mother.Id);
            Assert.Contains(Content.Events, e => e.Id == "evento.duelo.velorio" && e.With == EventDefinition.DeceasedRole);
        }

        [Fact]
        public void TuGente_LaAmistadQueMurio_SigueAhiComoFallecida()
        {
            var session = new LifeSession(Content, 6);
            World world = session.World;
            Character friend = Friend(world, 80);
            Dies(world, friend);

            PeopleView people = new LifePresenter(session).People();

            PersonRowView row = people.Close.Single(r => r.Id == friend.Id);
            Assert.False(row.IsAlive);
        }

        [Fact]
        public void GuardarYCargar_ConservaElDuelo()
        {
            var session = new LifeSession(Content, 7);
            World world = session.World;
            Grief grief = Dies(world, world.GetCharacter(world.Player.MotherId!.Value))!;
            grief.ReliefWeeks = 3;

            string saved = session.SaveToText();
            LifeSession loaded = LifeSession.Load(Content, saved);

            Grief again = loaded.World.Player.Griefs.Single();
            Assert.Equal(grief.PersonId, again.PersonId);
            Assert.Equal(grief.Weight, again.Weight);
            Assert.Equal("madre", again.Relation);
            Assert.Equal(3, again.ReliefWeeks);
            Assert.Equal(saved, loaded.SaveToText());
        }

        [Fact]
        public void DuelosViejos_SeOlvidanPasadoElPrimerAno()
        {
            var session = new LifeSession(Content, 8);
            World world = session.World;
            Character friend = Friend(world, 40);
            Dies(world, friend);

            new WeekCycle().Register(new GriefSystem()).AdvanceWeeks(world, GriefRules.RememberWeeks + 2);

            Assert.Empty(world.Player.Griefs);
        }
    }
}

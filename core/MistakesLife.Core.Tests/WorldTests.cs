using MistakesLife.Core.Characters;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class WorldTests
    {
        [Fact]
        public void VidaNueva_EmpiezaConUnRecienNacido()
        {
            World world = WorldFactory.CreateNewLife(1);
            Character player = world.Player;
            Assert.True(player.IsAlive);
            Assert.Equal(0, player.AgeInWeeks(world.Date));
            Assert.Contains(world.Journal, e => e.TextKey == "diario.nacimiento");
        }

        [Fact]
        public void VidaNueva_ConLaMismaSemilla_NaceIgual()
        {
            Character a = WorldFactory.CreateNewLife(77).Player;
            Character b = WorldFactory.CreateNewLife(77).Player;
            Assert.Equal(a.Sex, b.Sex);
            Assert.Equal(a.Personality.Openness, b.Personality.Openness);
            Assert.Equal(a.Personality.Neuroticism, b.Personality.Neuroticism);
            Assert.Equal(a.Stats.Health, b.Stats.Health);
        }

        [Fact]
        public void IdsDePersonajes_NoSeRepiten()
        {
            World world = new World(1);
            Assert.NotEqual(world.NextCharacterId(), world.NextCharacterId());
        }
    }
}

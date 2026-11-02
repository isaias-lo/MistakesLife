using MistakesLife.Core.Characters;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class CharacterTests
    {
        private static Character NewCharacter(GameDate birth) =>
            new Character(1, "nombre", "apellido", Sex.Female, birth, new Personality(50, 50, 50, 50, 50), new Stats(80, 80, 50));

        [Fact]
        public void Edad_SeCalculaEnSemanasYAnios()
        {
            var character = NewCharacter(new GameDate(10));
            var date = new GameDate(10 + (GameDate.WeeksPerYear * 3) + 5);
            Assert.Equal(3, character.AgeInYears(date));
            Assert.Equal((GameDate.WeeksPerYear * 3) + 5, character.AgeInWeeks(date));
        }

        [Fact]
        public void Estadisticas_SeLimitanEntreCeroYCien()
        {
            var stats = new Stats(150, -20, 50);
            Assert.Equal(100, stats.Health);
            Assert.Equal(0, stats.Happiness);
            stats.Smarts += 80;
            Assert.Equal(100, stats.Smarts);
        }

        [Fact]
        public void Personalidad_AlAzar_QuedaEntreCeroYCien()
        {
            var random = new GameRandom(123);
            for (int i = 0; i < 1000; i++)
            {
                Personality p = Personality.Random(random);
                foreach (int trait in new[] { p.Openness, p.Conscientiousness, p.Extraversion, p.Agreeableness, p.Neuroticism })
                {
                    Assert.InRange(trait, 0, 100);
                }
            }
        }

        [Fact]
        public void Morir_GuardaLaFechaUnaSolaVez()
        {
            var character = NewCharacter(GameDate.Start);
            character.Die(new GameDate(100));
            character.Die(new GameDate(200));
            Assert.False(character.IsAlive);
            Assert.Equal(new GameDate(100), character.DeathDate);
        }
    }
}

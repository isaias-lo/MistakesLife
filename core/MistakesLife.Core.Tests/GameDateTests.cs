using System;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class GameDateTests
    {
        [Fact]
        public void Inicio_EsAnioUnoSemanaUno()
        {
            Assert.Equal(1, GameDate.Start.Year);
            Assert.Equal(1, GameDate.Start.WeekOfYear);
        }

        [Theory]
        [InlineData(51, 1, 52)]
        [InlineData(52, 2, 1)]
        [InlineData(104, 3, 1)]
        public void SemanasTotales_SeConviertenEnAnioYSemana(int totalWeeks, int year, int weekOfYear)
        {
            var date = new GameDate(totalWeeks);
            Assert.Equal(year, date.Year);
            Assert.Equal(weekOfYear, date.WeekOfYear);
        }

        [Fact]
        public void SemanasNegativas_Fallan()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(-1));
        }

        [Fact]
        public void AddWeeks_YWeeksSince_SonConsistentes()
        {
            var start = new GameDate(10);
            var later = start.AddWeeks(30);
            Assert.Equal(30, later.WeeksSince(start));
            Assert.True(later > start);
        }
    }
}

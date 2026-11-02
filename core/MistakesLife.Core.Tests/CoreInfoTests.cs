using MistakesLife.Core;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class CoreInfoTests
    {
        [Fact]
        public void GameName_IsMistakesLife()
        {
            Assert.Equal("MistakesLife", CoreInfo.GameName);
        }

        [Fact]
        public void SaveFormatVersion_StartsAtOne()
        {
            Assert.True(CoreInfo.SaveFormatVersion >= 1);
        }
    }
}

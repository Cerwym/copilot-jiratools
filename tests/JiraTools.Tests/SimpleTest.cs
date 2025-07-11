using Xunit;

namespace JiraTools.Tests
{
    public class SimpleTest
    {
        [Fact]
        public void BasicTest_ShouldPass()
        {
            // A very simple test that should always pass
            Assert.Equal(2, 1 + 1);
        }
    }
}

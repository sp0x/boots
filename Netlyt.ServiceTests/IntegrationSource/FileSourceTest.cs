using Donut.IntegrationSource;
using Xunit;

namespace Netlyt.ServiceTests.IntegrationSource
{
    [Collection("Data Sources")]
    public class FileSourceTest
    {
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public void Shards(string file)
        {
            var source = new FileSource(file);
            Assert.NotEmpty(source.Shards());
        }
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public void ShardsKeys(string file)
        {
            var source = new FileSource(file);
            Assert.NotEmpty(source.ShardKeys());
        }
    }
}

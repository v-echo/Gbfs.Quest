using Gbfs.Quest.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Gbfs.Quest.Tests
{
    public class GbfsServiceIntegrationTests
    {
        readonly IServiceProvider ServiceProvider;

        public GbfsServiceIntegrationTests()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            services.AddSingleton<GbfsService>();

            ServiceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Fetch_Gbfs_Feeds_And_Generate_Questions()
        {
            var service = ServiceProvider.GetService<GbfsService>();
            var questions = await service!.GetQuestionsAsync(100);

            Assert.NotEmpty(questions);
            Assert.Equal(100, questions.Length);
        }
    }
}

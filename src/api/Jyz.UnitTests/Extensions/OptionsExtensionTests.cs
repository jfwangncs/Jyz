using Jyz.Extensions;
using Jyz.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jyz.UnitTests.Extensions
{
    public class OptionsExtensionTests
    {
        [Fact]
        public void AddCustomOptions_BindsJwtSettingsFromConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = "test-secret-key",
                    ["JwtSettings:Issuer"] = "test-issuer",
                    ["JwtSettings:Audience"] = "test-audience",
                    ["JwtSettings:Expires"] = "120",
                })
                .Build();
            var services = new ServiceCollection();

            services.AddCustomOptions(configuration);
            var provider = services.BuildServiceProvider();

            var settings = provider.GetRequiredService<IOptions<JwtSettings>>().Value;
            settings.SecretKey.Should().Be("test-secret-key");
            settings.Issuer.Should().Be("test-issuer");
            settings.Audience.Should().Be("test-audience");
            settings.Expires.Should().Be(120);
        }
    }
}

using APNCore;
using APNCore.Auth;
using APNService;
using APNService.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APNService.Tests;

public sealed class AddApnsTests
{
    /// <summary>Writes a freshly generated EC private key to a temporary <c>.p8</c> file.</summary>
    private static string WriteTempP8()
    {
        var path = Path.Combine(Path.GetTempPath(), $"AuthKey_{Guid.NewGuid():N}.p8");
        File.WriteAllText(path, TestKeys.NewPem());
        return path;
    }

    [Fact]
    public void AddApns_delegate_registers_IApnClient_as_a_singleton()
    {
        var p8 = WriteTempP8();
        try
        {
            var services = new ServiceCollection();
            services.AddApns(apns =>
            {
                apns.TeamId = "TEAM123456";
                apns.KeyId = "KEY1234567";
                apns.P8FilePath = p8;
                apns.DefaultTopic = "com.example.app";
            });

            using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<IApnClient>();
            Assert.IsType<ApnClient>(client);
            Assert.Same(client, provider.GetRequiredService<IApnClient>());
        }
        finally
        {
            File.Delete(p8);
        }
    }

    [Fact]
    public void AddApns_registers_the_jwt_provider_and_settings()
    {
        var p8 = WriteTempP8();
        try
        {
            var services = new ServiceCollection();
            services.AddApns(apns =>
            {
                apns.TeamId = "TEAM123456";
                apns.KeyId = "KEY1234567";
                apns.P8FilePath = p8;
                apns.Environment = ApnEnvironment.Production;
                apns.DefaultTopic = "com.example.app";
            });

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetRequiredService<ApnJwtProvider>());

            var settings = provider.GetRequiredService<ApnsSettings>();
            Assert.Equal("TEAM123456", settings.TeamId);
            Assert.Equal(ApnEnvironment.Production, settings.Environment);
            Assert.Equal("com.example.app", settings.DefaultTopic);
        }
        finally
        {
            File.Delete(p8);
        }
    }

    [Fact]
    public void AddApns_binds_settings_from_configuration()
    {
        var p8 = WriteTempP8();
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Apns:TeamId"] = "TEAM123456",
                    ["Apns:KeyId"] = "KEY1234567",
                    ["Apns:P8FilePath"] = p8,
                    ["Apns:Environment"] = "Production",
                    ["Apns:DefaultTopic"] = "com.example.app",
                    ["Apns:TimeoutSeconds"] = "45",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddApns(configuration.GetSection("Apns"));

            using var provider = services.BuildServiceProvider();

            var settings = provider.GetRequiredService<ApnsSettings>();
            Assert.Equal("KEY1234567", settings.KeyId);
            Assert.Equal(ApnEnvironment.Production, settings.Environment);
            Assert.Equal(45, settings.TimeoutSeconds);
            Assert.IsType<ApnClient>(provider.GetRequiredService<IApnClient>());
        }
        finally
        {
            File.Delete(p8);
        }
    }

    [Fact]
    public void AddApns_throws_when_a_required_setting_is_missing()
    {
        var services = new ServiceCollection();

        // KeyId and P8FilePath are left blank.
        Assert.Throws<ArgumentException>(() =>
            services.AddApns(apns => apns.TeamId = "TEAM123456"));
    }
}

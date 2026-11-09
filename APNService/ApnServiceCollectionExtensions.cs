using APNCore;
using APNCore.Auth;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency-injection extensions for registering APNs services.
/// </summary>
public static class ApnServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IApnClient"/> (and its supporting <see cref="ApnJwtProvider"/>) as
    /// singletons, configured by the supplied delegate.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">A delegate that populates the <see cref="ApnsSettings"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddApns(this IServiceCollection services, Action<ApnsSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var settings = new ApnsSettings();
        configure(settings);
        return AddApnsCore(services, settings);
    }

    /// <summary>
    /// Registers an <see cref="IApnClient"/> (and its supporting <see cref="ApnJwtProvider"/>) as
    /// singletons, bound from a configuration section (for example <c>Configuration.GetSection("Apns")</c>).
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The configuration section to bind an <see cref="ApnsSettings"/> from.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddApns(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = new ApnsSettings();
        configuration.Bind(settings);
        return AddApnsCore(services, settings);
    }

    private static IServiceCollection AddApnsCore(IServiceCollection services, ApnsSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.TeamId, "ApnsSettings.TeamId");
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.KeyId, "ApnsSettings.KeyId");
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.P8FilePath, "ApnsSettings.P8FilePath");

        services.AddSingleton(settings);

        // The provider and client are thread-safe and long-lived; the container disposes both.
        services.AddSingleton(_ =>
            ApnJwtProvider.FromP8File(settings.TeamId, settings.KeyId, settings.P8FilePath));

        services.AddSingleton<IApnClient>(sp => new APNService.ApnClient(new ApnClientOptions
        {
            JwtProvider = sp.GetRequiredService<ApnJwtProvider>(),
            Environment = settings.Environment,
            DefaultTopic = settings.DefaultTopic,
            UseAlternatePort = settings.UseAlternatePort,
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds),
        }));

        return services;
    }
}

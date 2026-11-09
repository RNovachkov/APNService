namespace APNCore;

/// <summary>
/// The APNs server environment to deliver notifications to.
/// </summary>
public enum ApnEnvironment
{
    /// <summary>
    /// The production environment (<c>https://api.push.apple.com</c>). Use this for apps
    /// distributed through TestFlight or the App Store.
    /// </summary>
    Production,

    /// <summary>
    /// The sandbox / development environment (<c>https://api.sandbox.push.apple.com</c>).
    /// Use this for apps built and signed with a development provisioning profile.
    /// </summary>
    Development,
}

/// <summary>Extensions for <see cref="ApnEnvironment"/>.</summary>
public static class ApnEnvironmentExtensions
{
    /// <summary>Returns the base <see cref="Uri"/> for the given environment.</summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="useAlternatePort">
    /// When <see langword="true"/>, uses port 2197 instead of 443. Port 2197 lets you send
    /// notifications through firewalls that block the standard HTTPS port.
    /// </param>
    public static Uri GetBaseUri(this ApnEnvironment environment, bool useAlternatePort = false)
    {
        var host = environment switch
        {
            ApnEnvironment.Production => "api.push.apple.com",
            ApnEnvironment.Development => "api.sandbox.push.apple.com",
            _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown APNs environment."),
        };

        var port = useAlternatePort ? 2197 : 443;
        return new Uri($"https://{host}:{port}");
    }
}

namespace APNCore;

/// <summary>
/// Bindable configuration for registering APNs services via dependency injection (for example from
/// an <c>Apns</c> section of <c>appsettings.json</c>). Mutable, with a parameterless constructor, so
/// it binds directly from <c>Microsoft.Extensions.Configuration</c>.
/// </summary>
public sealed class ApnsSettings
{
    /// <summary>Your 10-character Apple Developer Team ID (the JWT <c>iss</c> claim). Required.</summary>
    public string TeamId { get; set; } = "";

    /// <summary>The 10-character Key ID of the <c>.p8</c> key (the JWT <c>kid</c> header). Required.</summary>
    public string KeyId { get; set; } = "";

    /// <summary>Path to the <c>AuthKey_XXXXXXXXXX.p8</c> key file on disk. Required.</summary>
    public string P8FilePath { get; set; } = "";

    /// <summary>The target APNs environment. Defaults to <see cref="ApnEnvironment.Development"/>.</summary>
    public ApnEnvironment Environment { get; set; } = ApnEnvironment.Development;

    /// <summary>
    /// The default <c>apns-topic</c> (usually your app's bundle ID) applied to notifications that do
    /// not set their own topic.
    /// </summary>
    public string? DefaultTopic { get; set; }

    /// <summary>When <see langword="true"/>, uses port 2197 instead of 443.</summary>
    public bool UseAlternatePort { get; set; }

    /// <summary>The request timeout, in seconds, applied to the underlying HTTP client. Defaults to 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}

using System.Text.Json;
using System.Text.Json.Serialization;
using APNCore.Auth;

namespace APNCore;

/// <summary>
/// Configuration for an <see cref="ApnClient"/>.
/// </summary>
public sealed class ApnClientOptions
{
    /// <summary>The token provider that signs provider JWTs from your <c>.p8</c> key. Required.</summary>
    public required ApnJwtProvider JwtProvider { get; init; }

    /// <summary>The target APNs environment. Defaults to <see cref="ApnEnvironment.Development"/>.</summary>
    public ApnEnvironment Environment { get; init; } = ApnEnvironment.Development;

    /// <summary>
    /// The default <c>apns-topic</c> (usually your app's bundle ID) applied to any notification that
    /// does not set its own <see cref="Models.ApnNotification.Topic"/>.
    /// </summary>
    public string? DefaultTopic { get; init; }

    /// <summary>When <see langword="true"/>, uses port 2197 instead of 443.</summary>
    public bool UseAlternatePort { get; init; }

    /// <summary>The request timeout applied to the underlying <see cref="HttpClient"/>. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The JSON serializer options used for payloads. Defaults to omitting <see langword="null"/>
    /// values and camelCasing custom data keys (reserved <c>aps</c> keys keep their explicit names).
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; init; } = CreateDefaultJsonOptions();

    public static JsonSerializerOptions CreateDefaultJsonOptions() => new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

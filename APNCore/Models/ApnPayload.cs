using System.Text.Json;
using System.Text.Json.Nodes;

namespace APNCore.Models;

/// <summary>
/// The full JSON body of an APNs notification: the reserved <see cref="Aps"/> dictionary plus any
/// number of app-specific top-level keys.
/// </summary>
public sealed class ApnPayload
{
    /// <summary>The Apple-reserved <c>aps</c> dictionary.</summary>
    public Aps Aps { get; set; } = new();

    /// <summary>
    /// Custom, app-specific key/value pairs serialized at the top level of the payload, alongside
    /// <c>aps</c>. Keys must not be <c>aps</c>. Values are serialized with System.Text.Json.
    /// </summary>
    public IDictionary<string, object?>? CustomData { get; set; }

    /// <summary>Adds or overwrites a single top-level custom key, returning this payload for chaining.</summary>
    public ApnPayload WithCustom(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (string.Equals(key, "aps", StringComparison.Ordinal))
            throw new ArgumentException("'aps' is a reserved key and cannot be used as custom data.", nameof(key));

        CustomData ??= new Dictionary<string, object?>(StringComparer.Ordinal);
        CustomData[key] = value;
        return this;
    }

    /// <summary>Serializes the payload into a <see cref="JsonObject"/>, merging custom keys at the root.</summary>
    public JsonObject ToJsonNode(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var root = new JsonObject
        {
            ["aps"] = JsonSerializer.SerializeToNode(Aps, options),
        };

        if (CustomData is not null)
        {
            foreach (var (key, value) in CustomData)
            {
                if (string.Equals(key, "aps", StringComparison.Ordinal))
                    throw new InvalidOperationException("'aps' is a reserved key and cannot be used as custom data.");

                root[key] = JsonSerializer.SerializeToNode(value, options);
            }
        }

        return root;
    }

    /// <summary>Serializes the payload to its JSON string representation.</summary>
    public string ToJson(JsonSerializerOptions options) => ToJsonNode(options).ToJsonString(options);
}

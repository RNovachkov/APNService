using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace APNCore.Auth;

/// <summary>
/// Generates and caches the provider authentication token (a signed JWT) used for
/// token-based APNs authentication.
/// </summary>
/// <remarks>
/// <para>
/// The token is signed with the ES256 (ECDSA using P-256 and SHA-256) algorithm using the
/// private key contained in the <c>.p8</c> file downloaded from the Apple Developer portal.
/// </para>
/// <para>
/// Apple requires that a provider token be refreshed no more than once every 20 minutes and at
/// least once every 60 minutes. This provider generates a fresh token lazily and reuses it for
/// <see cref="RefreshInterval"/> (40 minutes), which sits safely inside that window. The instance
/// is thread-safe.
/// </para>
/// </remarks>
public sealed class ApnJwtProvider : IDisposable
{
    /// <summary>How long a generated token is reused before a new one is signed.</summary>
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(40);

    private readonly string _keyId;
    private readonly string _teamId;
    private readonly ECDsa _ecdsa;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _sync = new();

    private string? _cachedToken;
    private DateTimeOffset _generatedAt;
    private bool _disposed;

    /// <summary>
    /// Creates a provider from an already-loaded <see cref="ECDsa"/> key. The provider takes
    /// ownership of the key and disposes it when disposed.
    /// </summary>
    /// <param name="teamId">Your 10-character Apple Developer Team ID (the <c>iss</c> claim).</param>
    /// <param name="keyId">The 10-character Key ID of the <c>.p8</c> key (the <c>kid</c> header).</param>
    /// <param name="ecdsa">The P-256 ECDSA private key loaded from the <c>.p8</c> file.</param>
    /// <param name="timeProvider">Clock abstraction, mainly for testing. Defaults to the system clock.</param>
    public ApnJwtProvider(string teamId, string keyId, ECDsa ecdsa, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(ecdsa);

        _teamId = teamId;
        _keyId = keyId;
        _ecdsa = ecdsa;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Loads the private key from a <c>.p8</c> file on disk and creates a provider.
    /// </summary>
    /// <param name="teamId">Your 10-character Apple Developer Team ID.</param>
    /// <param name="keyId">The 10-character Key ID of the <c>.p8</c> key.</param>
    /// <param name="p8FilePath">Path to the <c>AuthKey_XXXXXXXXXX.p8</c> file.</param>
    public static ApnJwtProvider FromP8File(string teamId, string keyId, string p8FilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(p8FilePath);
        if (!File.Exists(p8FilePath))
            throw new FileNotFoundException("The APNs .p8 key file was not found.", p8FilePath);

        return FromP8Content(teamId, keyId, File.ReadAllText(p8FilePath));
    }

    /// <summary>
    /// Loads the private key from the PEM-encoded content of a <c>.p8</c> file.
    /// </summary>
    /// <param name="teamId">Your 10-character Apple Developer Team ID.</param>
    /// <param name="keyId">The 10-character Key ID of the <c>.p8</c> key.</param>
    /// <param name="p8Content">The full PEM text, including the <c>-----BEGIN PRIVATE KEY-----</c> markers.</param>
    public static ApnJwtProvider FromP8Content(string teamId, string keyId, string p8Content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(p8Content);

        var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(p8Content);
        }
        catch (Exception ex)
        {
            ecdsa.Dispose();
            throw new ArgumentException("The supplied .p8 content is not a valid PEM-encoded EC private key.", nameof(p8Content), ex);
        }

        return new ApnJwtProvider(teamId, keyId, ecdsa);
    }

    /// <summary>
    /// Returns a valid provider token, generating (and caching) a new one only when the current
    /// token has aged past <see cref="RefreshInterval"/>.
    /// </summary>
    public string GetToken()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            var now = _timeProvider.GetUtcNow();
            if (_cachedToken is not null && now - _generatedAt < RefreshInterval)
                return _cachedToken;

            _cachedToken = Generate(now);
            _generatedAt = now;
            return _cachedToken;
        }
    }

    private string Generate(DateTimeOffset now)
    {
        // Header: {"alg":"ES256","kid":"<keyId>"}
        var header = $$"""{"alg":"ES256","kid":"{{_keyId}}"}""";
        // Payload: {"iss":"<teamId>","iat":<unix-seconds>}
        var payload = $$"""{"iss":"{{_teamId}}","iat":{{now.ToUnixTimeSeconds()}}}""";

        var headerSegment = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(header));
        var payloadSegment = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payload));

        var signingInput = $"{headerSegment}.{payloadSegment}";

        // ES256 requires the raw R||S (IEEE P1363) signature, not the ASN.1/DER encoding.
        var signature = _ecdsa.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return $"{signingInput}.{Base64Url.EncodeToString(signature)}";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _ecdsa.Dispose();
    }
}

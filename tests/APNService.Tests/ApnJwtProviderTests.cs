using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using APNCore.Auth;
using APNService.Tests.Helpers;
using Xunit;

namespace APNService.Tests;

public sealed class ApnJwtProviderTests
{
    [Fact]
    public void GetToken_produces_three_base64url_segments()
    {
        var (provider, key) = TestKeys.NewProvider();
        using (provider)
        using (key)
        {
            var token = provider.GetToken();

            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
            Assert.All(parts, p => Assert.DoesNotContain('+', p));
            Assert.All(parts, p => Assert.DoesNotContain('/', p));
            Assert.All(parts, p => Assert.DoesNotContain('=', p));
        }
    }

    [Fact]
    public void Token_header_declares_ES256_and_key_id()
    {
        var (provider, key) = TestKeys.NewProvider(keyId: "ABCDE12345");
        using (provider)
        using (key)
        {
            var header = DecodeSegment(provider.GetToken(), 0);

            using var doc = JsonDocument.Parse(header);
            Assert.Equal("ES256", doc.RootElement.GetProperty("alg").GetString());
            Assert.Equal("ABCDE12345", doc.RootElement.GetProperty("kid").GetString());
        }
    }

    [Fact]
    public void Token_payload_contains_issuer_and_issued_at()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        var (provider, key) = TestKeys.NewProvider(teamId: "MYTEAM0001", timeProvider: clock);
        using (provider)
        using (key)
        {
            var payload = DecodeSegment(provider.GetToken(), 1);

            using var doc = JsonDocument.Parse(payload);
            Assert.Equal("MYTEAM0001", doc.RootElement.GetProperty("iss").GetString());
            Assert.Equal(1_700_000_000, doc.RootElement.GetProperty("iat").GetInt64());
        }
    }

    [Fact]
    public void Signature_is_valid_ES256_over_the_signing_input()
    {
        var (provider, key) = TestKeys.NewProvider();
        using (provider)
        using (key)
        {
            var token = provider.GetToken();
            var parts = token.Split('.');

            var signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
            var signature = Base64Url.DecodeFromChars(parts[2]);

            var valid = key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            Assert.True(valid);
        }
    }

    [Fact]
    public void GetToken_reuses_cached_token_within_refresh_window()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var (provider, key) = TestKeys.NewProvider(timeProvider: clock);
        using (provider)
        using (key)
        {
            var first = provider.GetToken();
            clock.Advance(TimeSpan.FromMinutes(39));
            var second = provider.GetToken();

            Assert.Same(first, second);
        }
    }

    [Fact]
    public void GetToken_regenerates_after_refresh_interval()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        var (provider, key) = TestKeys.NewProvider(timeProvider: clock);
        using (provider)
        using (key)
        {
            var first = provider.GetToken();
            clock.Advance(TimeSpan.FromMinutes(41));
            var second = provider.GetToken();

            Assert.NotEqual(first, second);
        }
    }

    [Fact]
    public void FromP8File_throws_when_file_is_missing()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ApnJwtProvider.FromP8File("TEAM123456", "KEY1234567", "/no/such/key.p8"));
    }

    [Fact]
    public void FromP8Content_throws_on_invalid_pem()
    {
        Assert.Throws<ArgumentException>(() =>
            ApnJwtProvider.FromP8Content("TEAM123456", "KEY1234567", "not a pem"));
    }

    [Fact]
    public void FromP8Content_loads_a_valid_pkcs8_pem()
    {
        using var provider = ApnJwtProvider.FromP8Content("TEAM123456", "KEY1234567", TestKeys.NewPem());
        Assert.False(string.IsNullOrWhiteSpace(provider.GetToken()));
    }

    [Fact]
    public void GetToken_throws_after_dispose()
    {
        var (provider, key) = TestKeys.NewProvider();
        using (key)
        {
            provider.Dispose();
            Assert.Throws<ObjectDisposedException>(provider.GetToken);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_blank_team_id(string? teamId)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Assert.ThrowsAny<ArgumentException>(() => new ApnJwtProvider(teamId!, "KEY1234567", ecdsa));
    }

    private static string DecodeSegment(string token, int index)
    {
        var segment = token.Split('.')[index];
        return Encoding.UTF8.GetString(Base64Url.DecodeFromChars(segment));
    }
}

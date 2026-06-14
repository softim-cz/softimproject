using System.Security.Cryptography;
using System.Text;

namespace SoftimProject.Application.Common;

/// <summary>
/// Hashing + generation for personal API keys. Keys are high-entropy random
/// secrets, so a fast cryptographic hash (SHA-256) is sufficient and lets us
/// look them up by hash. Only the hash is persisted.
/// </summary>
public static class ApiKeyHasher
{
    public const string Prefix = "spk_";

    /// <summary>Generates a new opaque key, returning the plaintext (shown once).</summary>
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var body = Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return Prefix + body;
    }

    /// <summary>SHA-256 (uppercase hex) of the full plaintext key.</summary>
    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    /// <summary>Non-secret display prefix for the UI (e.g. "spk_AbCdEf…").</summary>
    public static string DisplayPrefix(string key) =>
        key.Length <= 10 ? key : key[..10] + "…";
}

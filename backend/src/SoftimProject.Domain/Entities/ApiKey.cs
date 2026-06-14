using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

/// <summary>
/// A personal API key for headless/programmatic API access. The full key is
/// shown only once at creation; only its SHA-256 hash is stored. Requests
/// authenticate as the owning user, inheriting their permissions.
/// </summary>
public class ApiKey : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>User-given label, e.g. "Postman", "CI script".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>SHA-256 (hex) of the full plaintext key. Lookup key.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>Non-secret display prefix (e.g. "spk_AbCd…") for the UI list.</summary>
    public string Prefix { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

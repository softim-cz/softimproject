namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Encrypts/decrypts small secrets (e.g. integration API tokens) for storage at rest.
/// Implemented over ASP.NET Data Protection — the pattern already used in this codebase
/// for GitHub OAuth tokens.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Encrypts a plaintext secret. Returns null for null/empty input.</summary>
    string? Protect(string? plaintext);

    /// <summary>Decrypts a value produced by <see cref="Protect"/>. Returns null for null/empty input.</summary>
    string? Unprotect(string? ciphertext);
}

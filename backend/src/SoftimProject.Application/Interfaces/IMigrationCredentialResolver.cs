namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Resolves the effective credentials for a migration/import step. When a saved connection is
/// chosen in the wizard, its <c>BaseUrl</c> and decrypted API token are used so the user does
/// not re-enter them (the token is never returned to the client). Otherwise the supplied
/// values are passed through unchanged.
/// </summary>
public interface IMigrationCredentialResolver
{
    Task<(string BaseUrl, string ApiKey)> ResolveAsync(string? baseUrl, string? apiKey, Guid? connectionId, CancellationToken ct);
}

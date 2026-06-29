using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Services.Integrations;

/// <inheritdoc />
public sealed class MigrationCredentialResolver(IApplicationDbContext dbContext, ISecretProtector protector)
    : IMigrationCredentialResolver
{
    public async Task<(string BaseUrl, string ApiKey)> ResolveAsync(string? baseUrl, string? apiKey, Guid? connectionId, CancellationToken ct)
    {
        if (connectionId is not { } id)
            return (baseUrl ?? string.Empty, apiKey ?? string.Empty);

        var connection = await dbContext.IntegrationConnections
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(IntegrationConnection), id);

        var token = protector.Unprotect(connection.EncryptedApiToken);
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Uložené připojení nemá použitelný API token.");

        return (connection.BaseUrl, token);
    }
}

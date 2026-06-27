using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.Integrations;

public sealed class IntegrationConnectionWriter(IApplicationDbContext dbContext, ISecretProtector protector)
    : IIntegrationConnectionWriter
{
    public async Task<Guid> UpsertForEasyProjectAsync(StartMigrationCommand command, CancellationToken ct)
    {
        const SyncType system = SyncType.EasyProject;

        var mappingsJson = JsonSerializer.Serialize(new StoredConnectionMappings(
            command.TrackerMapping,
            command.StatusMapping,
            command.PriorityMapping,
            command.UserMapping,
            command.AutoCreateTrackers,
            command.AutoCreateStatuses,
            command.AutoCreateStatusIsClosed,
            command.AutoCreatePriorities));
        var optionsJson = JsonSerializer.Serialize(new StoredConnectionOptions(
            command.SkipClosedIssues,
            command.SkipAttachments,
            command.ImportComments,
            command.ImportWorklogs,
            command.ImportChecklists,
            command.CreateMissingUsers));
        var selectorJson = JsonSerializer.Serialize(command.ProjectIds);
        var encryptedToken = protector.Protect(command.ApiKey);

        var existing = await dbContext.IntegrationConnections
            .FirstOrDefaultAsync(c => c.SourceSystem == system && c.BaseUrl == command.BaseUrl, ct);

        if (existing != null)
        {
            // Refresh credentials + "what to sync"; preserve scheduling/policy the user owns.
            existing.EncryptedApiToken = encryptedToken;
            existing.TargetProjectTemplateId = command.TargetProjectTemplateId;
            existing.MappingsJson = mappingsJson;
            existing.OptionsJson = optionsJson;
            existing.ProjectSelectorJson = selectorJson;
            await dbContext.SaveChangesAsync(ct);
            return existing.Id;
        }

        var connection = new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            Name = $"EasyProject ({SafeHost(command.BaseUrl)})",
            SourceSystem = system,
            BaseUrl = command.BaseUrl,
            EncryptedApiToken = encryptedToken,
            TargetProjectTemplateId = command.TargetProjectTemplateId,
            ConflictPolicy = ConflictPolicy.SourceOwnedWins,
            Mode = IntegrationSyncMode.Manual,
            IntervalMinutes = 1440,
            IsEnabled = false,
            MappingsJson = mappingsJson,
            OptionsJson = optionsJson,
            ProjectSelectorJson = selectorJson,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.IntegrationConnections.Add(connection);
        await dbContext.SaveChangesAsync(ct);
        return connection.Id;
    }

    private static string SafeHost(string baseUrl)
        => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri.Host : baseUrl;
}

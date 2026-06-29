using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Integration;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.Integrations;

public sealed class IntegrationConnectionWriter(IApplicationDbContext dbContext, ISecretProtector protector)
    : IIntegrationConnectionWriter
{
    // EasyProject migration path (int-keyed command) — stringifies the keys onto the
    // provider-agnostic stored shape.
    public Task<Guid> UpsertForEasyProjectAsync(StartMigrationCommand command, Guid createdByUserId, CancellationToken ct)
    {
        var mappings = new StoredConnectionMappings(
            command.TrackerMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            command.StatusMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            command.PriorityMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            command.UserMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            command.AutoCreateTrackers?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            command.AutoCreateStatuses?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            command.AutoCreateStatusIsClosed?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            command.AutoCreatePriorities?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
        var options = new StoredConnectionOptions(
            command.SkipClosedIssues, command.SkipAttachments, command.ImportComments,
            command.ImportWorklogs, command.ImportChecklists, command.CreateMissingUsers);

        return UpsertCoreAsync(
            SyncType.EasyProject, command.BaseUrl, command.ApiKey, command.TargetProjectTemplateId,
            command.TargetCompanyId, command.EnableIncrementalSync, command.SyncIntervalMinutes,
            command.ProjectIds.Select(id => id.ToString()).ToList(),
            mappings, options, createdByUserId, ct);
    }

    // Provider-agnostic import path (string-keyed canonical command).
    public Task<Guid> UpsertForImportAsync(StartSourceImportCommand command, Guid createdByUserId, CancellationToken ct)
    {
        var mappings = new StoredConnectionMappings(
            command.TrackerMapping, command.StatusMapping, command.PriorityMapping, command.UserMapping,
            command.AutoCreateTrackers, command.AutoCreateStatuses, command.AutoCreateStatusIsClosed, command.AutoCreatePriorities);
        var options = new StoredConnectionOptions(
            command.SkipClosedIssues, command.SkipAttachments, command.ImportComments,
            command.ImportWorklogs, command.ImportChecklists, command.CreateMissingUsers);

        return UpsertCoreAsync(
            command.SourceSystem, command.BaseUrl, command.ApiKey, command.TargetProjectTemplateId,
            command.TargetCompanyId, command.EnableIncrementalSync, command.SyncIntervalMinutes,
            command.ProjectExternalIds, mappings, options, createdByUserId, ct);
    }

    // Remember-on-test: persist the bare connection (system + URL + token) the moment it is
    // verified, independent of any later import. An existing connection only gets its token
    // refreshed so its template/mappings/scheduling stay intact.
    public async Task<Guid> RememberConnectionAsync(SyncType system, string baseUrl, string apiKey, Guid createdByUserId, CancellationToken ct)
    {
        var encryptedToken = protector.Protect(apiKey);

        var existing = await dbContext.IntegrationConnections
            .FirstOrDefaultAsync(c => c.SourceSystem == system && c.BaseUrl == baseUrl, ct);

        if (existing != null)
        {
            existing.EncryptedApiToken = encryptedToken;
            await dbContext.SaveChangesAsync(ct);
            return existing.Id;
        }

        var connection = new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            Name = $"{system} ({SafeHost(baseUrl)})",
            SourceSystem = system,
            BaseUrl = baseUrl,
            EncryptedApiToken = encryptedToken,
            TargetProjectTemplateId = null,
            CreatedByUserId = createdByUserId,
            ConflictPolicy = ConflictPolicy.SourceOwnedWins,
            Mode = IntegrationSyncMode.Manual,
            IsEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.IntegrationConnections.Add(connection);
        await dbContext.SaveChangesAsync(ct);
        return connection.Id;
    }

    private async Task<Guid> UpsertCoreAsync(
        SyncType system, string baseUrl, string apiKey, Guid templateId, Guid? companyId,
        bool enableIncremental, int intervalMinutes, List<string> projectExternalIds,
        StoredConnectionMappings mappings, StoredConnectionOptions options, Guid createdByUserId, CancellationToken ct)
    {
        var mappingsJson = JsonSerializer.Serialize(mappings);
        var optionsJson = JsonSerializer.Serialize(options);
        var selectorJson = JsonSerializer.Serialize(projectExternalIds);
        var encryptedToken = protector.Protect(apiKey);

        // The wizard is the source of truth for scheduling: enabling incremental sync sets
        // Mode/IsEnabled, otherwise it stays manual (one-time import).
        var mode = enableIncremental ? IntegrationSyncMode.FullThenIncremental : IntegrationSyncMode.Manual;

        var existing = await dbContext.IntegrationConnections
            .FirstOrDefaultAsync(c => c.SourceSystem == system && c.BaseUrl == baseUrl, ct);

        if (existing != null)
        {
            existing.EncryptedApiToken = encryptedToken;
            existing.TargetProjectTemplateId = templateId;
            existing.TargetCompanyId = companyId;
            existing.MappingsJson = mappingsJson;
            existing.OptionsJson = optionsJson;
            existing.ProjectSelectorJson = selectorJson;
            existing.Mode = mode;
            existing.IsEnabled = enableIncremental;
            existing.IntervalMinutes = intervalMinutes;
            await dbContext.SaveChangesAsync(ct);
            return existing.Id;
        }

        var connection = new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            Name = $"{system} ({SafeHost(baseUrl)})",
            SourceSystem = system,
            BaseUrl = baseUrl,
            EncryptedApiToken = encryptedToken,
            TargetProjectTemplateId = templateId,
            TargetCompanyId = companyId,
            CreatedByUserId = createdByUserId,
            ConflictPolicy = ConflictPolicy.SourceOwnedWins,
            Mode = mode,
            IntervalMinutes = intervalMinutes,
            IsEnabled = enableIncremental,
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

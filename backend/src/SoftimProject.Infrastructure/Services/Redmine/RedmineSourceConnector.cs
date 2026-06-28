using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.Services.Redmine;

/// <summary>
/// <see cref="ISourceConnector"/> for vanilla Redmine. Reuses the shared
/// <see cref="RedmineFamilyConnector"/> (same REST shape as EasyProject); only the system tag
/// differs. EasyProject-only fields are simply absent and map to empty/false.
/// </summary>
public sealed class RedmineSourceConnector(IEasyProjectApiClient apiClient, ILogger<RedmineSourceConnector> logger)
    : RedmineFamilyConnector(apiClient, logger)
{
    public override SyncType SourceSystem => SyncType.Redmine;
}

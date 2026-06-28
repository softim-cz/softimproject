using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.Services.EasyProject;

/// <summary>
/// <see cref="ISourceConnector"/> for EasyProject. EasyProject is a Redmine derivative, so the
/// shared <see cref="RedmineFamilyConnector"/> provides all behavior; only the system tag differs.
/// </summary>
public sealed class EasyProjectSourceConnector(IEasyProjectApiClient apiClient, ILogger<EasyProjectSourceConnector> logger)
    : RedmineFamilyConnector(apiClient, logger)
{
    public override SyncType SourceSystem => SyncType.EasyProject;
}

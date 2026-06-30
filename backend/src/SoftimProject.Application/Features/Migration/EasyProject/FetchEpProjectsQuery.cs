using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record FetchEpProjectsQuery(string? BaseUrl, string? ApiKey, Guid? ConnectionId = null) : IRequest<List<EpProjectPreviewDto>>;

public sealed class FetchEpProjectsQueryHandler(
    IEasyProjectApiClient apiClient,
    IMigrationCredentialResolver credentials,
    IApplicationDbContext dbContext) : IRequestHandler<FetchEpProjectsQuery, List<EpProjectPreviewDto>>
{
    public async Task<List<EpProjectPreviewDto>> Handle(FetchEpProjectsQuery request, CancellationToken cancellationToken)
    {
        var (baseUrl, apiKey) = await credentials.ResolveAsync(request.BaseUrl, request.ApiKey, request.ConnectionId, cancellationToken);
        var epProjects = await apiClient.GetProjectsAsync(baseUrl, apiKey, cancellationToken);

        var importedExternalIds = await dbContext.Projects
            .Where(p => p.ExternalSystem == "EasyProject" && p.ExternalProjectId != null)
            .Select(p => p.ExternalProjectId!)
            .ToListAsync(cancellationToken);

        var importedSet = importedExternalIds.ToHashSet();

        var result = epProjects.Select(ep =>
        {
            var parentName = ep.Parent != null
                ? epProjects.FirstOrDefault(p => p.Id == ep.Parent.Id)?.Name
                : null;
            return new EpProjectPreviewDto(
                ep.Id, ep.Name, ep.Description, ep.Status,
                parentName, -1,
                importedSet.Contains(ep.Id.ToString()));
        }).ToList();

        return result;
    }
}

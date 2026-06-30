using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record EpUserMappingDto(
    int EpId, string EpName, string? EpEmail,
    Guid? MatchedUserId, string? MatchedUserName);

public sealed record FetchEpUsersQuery(string? BaseUrl, string? ApiKey, Guid? ConnectionId = null) : IRequest<List<EpUserMappingDto>>;

public sealed class FetchEpUsersQueryHandler(
    IEasyProjectApiClient apiClient,
    IMigrationCredentialResolver credentials,
    IApplicationDbContext dbContext) : IRequestHandler<FetchEpUsersQuery, List<EpUserMappingDto>>
{
    public async Task<List<EpUserMappingDto>> Handle(FetchEpUsersQuery request, CancellationToken cancellationToken)
    {
        var (baseUrl, apiKey) = await credentials.ResolveAsync(request.BaseUrl, request.ApiKey, request.ConnectionId, cancellationToken);
        var epUsers = await apiClient.GetUsersAsync(baseUrl, apiKey, cancellationToken);
        var spUsers = await dbContext.Users.Where(u => u.IsActive).ToListAsync(cancellationToken);

        var result = epUsers.Select(ep =>
        {
            var name = $"{ep.Firstname} {ep.Lastname}".Trim();
            var mail = ep.Mail?.ToLowerInvariant();

            // Try matching by email
            var match = mail != null
                ? spUsers.FirstOrDefault(u => u.Email.ToLowerInvariant() == mail)
                : null;

            return new EpUserMappingDto(
                ep.Id,
                string.IsNullOrEmpty(name) ? ep.Login ?? $"User #{ep.Id}" : name,
                ep.Mail,
                match?.Id,
                match?.DisplayName);
        }).ToList();

        return result;
    }
}

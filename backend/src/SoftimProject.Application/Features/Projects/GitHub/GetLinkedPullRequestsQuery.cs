using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record LinkedPullRequestDto(
    Guid Id,
    string Provider,
    string ExternalId,
    string Url,
    string Title,
    string Branch,
    string? AuthorLogin,
    PullRequestState State,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    DateTime? MergedAt);

public sealed record GetLinkedPullRequestsQuery(Guid ProjectId, Guid TicketId)
    : IRequest<List<LinkedPullRequestDto>>, IRequireProjectAccess;

public sealed class GetLinkedPullRequestsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetLinkedPullRequestsQuery, List<LinkedPullRequestDto>>
{
    public async Task<List<LinkedPullRequestDto>> Handle(
        GetLinkedPullRequestsQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.LinkedPullRequests
            .AsNoTracking()
            .Where(lp => lp.TicketId == request.TicketId && lp.Ticket!.ProjectId == request.ProjectId)
            .OrderByDescending(lp => lp.OpenedAt)
            .Select(lp => new LinkedPullRequestDto(
                lp.Id,
                lp.Provider,
                lp.ExternalId,
                lp.Url,
                lp.Title,
                lp.Branch,
                lp.AuthorLogin,
                lp.State,
                lp.OpenedAt,
                lp.ClosedAt,
                lp.MergedAt))
            .ToListAsync(cancellationToken);
    }
}

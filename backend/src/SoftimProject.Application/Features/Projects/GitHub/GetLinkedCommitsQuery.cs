using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record LinkedCommitDto(
    Guid Id,
    string Provider,
    string Sha,
    string Message,
    string Url,
    string? AuthorLogin,
    DateTime CommittedAt);

public sealed record GetLinkedCommitsQuery(Guid ProjectId, Guid TicketId)
    : IRequest<List<LinkedCommitDto>>, IRequireProjectAccess;

public sealed class GetLinkedCommitsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetLinkedCommitsQuery, List<LinkedCommitDto>>
{
    public async Task<List<LinkedCommitDto>> Handle(GetLinkedCommitsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.LinkedCommits
            .AsNoTracking()
            .Where(c => c.TicketId == request.TicketId && c.Ticket!.ProjectId == request.ProjectId)
            .OrderByDescending(c => c.CommittedAt)
            .Select(c => new LinkedCommitDto(
                c.Id,
                c.Provider,
                c.Sha,
                c.Message,
                c.Url,
                c.AuthorLogin,
                c.CommittedAt))
            .ToListAsync(cancellationToken);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record GetMigrationHistoryQuery : IRequest<List<MigrationJobDto>>;

public sealed class GetMigrationHistoryQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetMigrationHistoryQuery, List<MigrationJobDto>>
{
    public async Task<List<MigrationJobDto>> Handle(GetMigrationHistoryQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.MigrationJobs
            .OrderByDescending(j => j.StartedAt)
            .Select(j => new MigrationJobDto(
                j.Id,
                j.SourceSystem,
                j.SourceBaseUrl,
                j.Status.ToString(),
                j.StartedAt,
                j.CompletedAt,
                j.ProjectsMigrated,
                j.TicketsMigrated,
                j.ItemsFailed))
            .ToListAsync(cancellationToken);
    }
}

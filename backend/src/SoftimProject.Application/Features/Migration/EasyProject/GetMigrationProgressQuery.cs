using MediatR;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record GetMigrationProgressQuery(Guid JobId) : IRequest<MigrationProgressDto?>;

public sealed class GetMigrationProgressQueryHandler(
    IMigrationProgressTracker progressTracker) : IRequestHandler<GetMigrationProgressQuery, MigrationProgressDto?>
{
    public Task<MigrationProgressDto?> Handle(GetMigrationProgressQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(progressTracker.GetProgress(request.JobId));
    }
}

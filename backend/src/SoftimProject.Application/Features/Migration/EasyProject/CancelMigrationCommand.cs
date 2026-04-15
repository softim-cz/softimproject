using MediatR;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record CancelMigrationCommand(Guid JobId) : IRequest;

public sealed class CancelMigrationCommandHandler(
    IMigrationProgressTracker progressTracker) : IRequestHandler<CancelMigrationCommand>
{
    public Task Handle(CancelMigrationCommand request, CancellationToken cancellationToken)
    {
        progressTracker.RequestCancellation(request.JobId);
        return Task.CompletedTask;
    }
}

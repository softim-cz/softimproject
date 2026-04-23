using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Kanban.UpdateBoard;

public sealed record UpdateBoardCommand(
    Guid ProjectId,
    Guid BoardId,
    string Name) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class UpdateBoardCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateBoardCommand>
{
    public async Task Handle(UpdateBoardCommand request, CancellationToken cancellationToken)
    {
        var board = await dbContext.KanbanBoards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && b.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.KanbanBoard), request.BoardId);

        board.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

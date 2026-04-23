using MediatR;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Kanban.CreateBoard;

public sealed record CreateBoardCommand(
    Guid ProjectId,
    string Name) : IRequest<Guid>, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class CreateBoardCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<CreateBoardCommand, Guid>
{
    public async Task<Guid> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        var board = new KanbanBoard
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.KanbanBoards.Add(board);
        await dbContext.SaveChangesAsync(cancellationToken);

        return board.Id;
    }
}

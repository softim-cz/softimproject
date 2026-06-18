using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Kanban.GetBoard;

public sealed record GetBoardQuery(Guid BoardId, Guid ProjectId) : IRequest<BoardDto>, IRequireProjectAccess;

public sealed class GetBoardQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetBoardQuery, BoardDto>
{
    public async Task<BoardDto> Handle(GetBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await dbContext.KanbanBoards
            .AsNoTracking()
            .Where(b => b.Id == request.BoardId && b.ProjectId == request.ProjectId)
            .Select(BoardProjections.Detail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.KanbanBoard), request.BoardId);

        return board;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Kanban.GetBoard;

public sealed record GetDefaultBoardQuery(Guid ProjectId) : IRequest<BoardDto>, IRequireProjectAccess;

public sealed class GetDefaultBoardQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetDefaultBoardQuery, BoardDto>
{
    public async Task<BoardDto> Handle(GetDefaultBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await dbContext.KanbanBoards
            .AsNoTracking()
            .Where(b => b.ProjectId == request.ProjectId)
            .OrderByDescending(b => b.IsDefault)
            .ThenBy(b => b.CreatedAt)
            .Select(BoardProjections.Detail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("KanbanBoard", $"default for project {request.ProjectId}");

        return board;
    }
}

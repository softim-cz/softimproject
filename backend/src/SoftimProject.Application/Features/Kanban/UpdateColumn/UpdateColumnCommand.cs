using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Kanban.UpdateColumn;

public sealed record UpdateColumnCommand(
    Guid ProjectId,
    Guid BoardId,
    Guid ColumnId,
    string Name,
    int? WipLimit,
    TicketStatus MapsToStatus) : IRequest, IRequireProjectAccess;

public sealed class UpdateColumnCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateColumnCommand>
{
    public async Task Handle(UpdateColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await dbContext.KanbanColumns
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == request.BoardId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.KanbanColumn), request.ColumnId);

        column.Name = request.Name;
        column.WipLimit = request.WipLimit;
        column.MapsToStatus = request.MapsToStatus;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

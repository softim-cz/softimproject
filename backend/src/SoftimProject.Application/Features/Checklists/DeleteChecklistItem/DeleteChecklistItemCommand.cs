using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Checklists.DeleteChecklistItem;

public sealed record DeleteChecklistItemCommand(
    Guid ProjectId,
    Guid TicketId,
    Guid ItemId) : IRequest, IRequireProjectAccess;

public sealed class DeleteChecklistItemCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<DeleteChecklistItemCommand>
{
    public async Task Handle(DeleteChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.ChecklistItems
            .FirstOrDefaultAsync(ci => ci.Id == request.ItemId && ci.TicketId == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ChecklistItem), request.ItemId);

        dbContext.ChecklistItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

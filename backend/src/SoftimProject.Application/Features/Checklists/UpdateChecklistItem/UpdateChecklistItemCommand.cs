using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Checklists.UpdateChecklistItem;

public sealed record UpdateChecklistItemCommand(
    Guid ProjectId,
    Guid TicketId,
    Guid ItemId,
    string Text,
    bool IsCompleted,
    int Position) : IRequest, IRequireProjectAccess;

public sealed class UpdateChecklistItemCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateChecklistItemCommand>
{
    public async Task Handle(UpdateChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.ChecklistItems
            .FirstOrDefaultAsync(ci => ci.Id == request.ItemId && ci.TicketId == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ChecklistItem), request.ItemId);

        item.Text = request.Text;
        item.IsCompleted = request.IsCompleted;
        item.Position = request.Position;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;
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
        var item = await dbContext.GetChecklistItemForProjectAsync(
            request.ProjectId,
            request.TicketId,
            request.ItemId,
            cancellationToken);

        dbContext.ChecklistItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

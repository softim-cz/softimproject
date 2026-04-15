using MediatR;
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
        var item = await dbContext.GetChecklistItemForProjectAsync(
            request.ProjectId,
            request.TicketId,
            request.ItemId,
            cancellationToken);

        item.Text = request.Text;
        item.IsCompleted = request.IsCompleted;
        item.Position = request.Position;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

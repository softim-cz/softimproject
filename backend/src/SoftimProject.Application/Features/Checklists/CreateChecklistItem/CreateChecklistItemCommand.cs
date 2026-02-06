using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Checklists.CreateChecklistItem;

public sealed record CreateChecklistItemCommand(
    Guid ProjectId,
    Guid TicketId,
    string Text) : IRequest<Guid>, IRequireProjectAccess;

public sealed class CreateChecklistItemCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<CreateChecklistItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var maxPosition = await dbContext.ChecklistItems
            .Where(ci => ci.TicketId == request.TicketId)
            .Select(ci => (int?)ci.Position)
            .MaxAsync(cancellationToken) ?? -1;

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            Text = request.Text,
            IsCompleted = false,
            Position = maxPosition + 1,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ChecklistItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}

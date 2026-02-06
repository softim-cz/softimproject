using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Attachments.DeleteAttachment;

public sealed record DeleteAttachmentCommand(
    Guid ProjectId,
    Guid TicketId,
    Guid AttachmentId) : IRequest, IRequireProjectAccess;

public sealed class DeleteAttachmentCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService) : IRequestHandler<DeleteAttachmentCommand>
{
    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.TicketId == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.TicketAttachment), request.AttachmentId);

        // Extract blob name from URL for deletion
        var blobName = new Uri(attachment.BlobUrl).AbsolutePath.TrimStart('/');
        await blobStorageService.DeleteAsync("ticket-attachments", blobName, cancellationToken);

        dbContext.TicketAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;
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
        var attachment = await dbContext.GetTicketAttachmentForProjectAsync(
            request.ProjectId,
            request.TicketId,
            request.AttachmentId,
            cancellationToken);

        var blobName = new Uri(attachment.BlobUrl).AbsolutePath.TrimStart('/');
        await blobStorageService.DeleteAsync("ticket-attachments", blobName, cancellationToken);

        dbContext.TicketAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

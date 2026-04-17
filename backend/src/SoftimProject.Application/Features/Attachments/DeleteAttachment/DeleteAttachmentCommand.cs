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
    IBlobStorageService blobStorageService,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteAttachmentCommand>
{
    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.GetTicketAttachmentForProjectAsync(
            request.ProjectId,
            request.TicketId,
            request.AttachmentId,
            cancellationToken);

        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        if (attachment.UploadedById != userId
            && !currentUserService.IsInRole("Admin")
            && !currentUserService.IsInRole("Manager"))
        {
            throw new UnauthorizedAccessException("Only the uploader, Admin or Manager can delete this attachment.");
        }

        var blobName = new Uri(attachment.BlobUrl).AbsolutePath.TrimStart('/');
        await blobStorageService.DeleteAsync("ticket-attachments", blobName, cancellationToken);

        dbContext.TicketAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

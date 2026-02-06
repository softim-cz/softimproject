using MediatR;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Attachments.UploadAttachment;

public sealed record UploadAttachmentCommand(
    Guid ProjectId,
    Guid TicketId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream) : IRequest<Guid>, IRequireProjectAccess;

public sealed class UploadAttachmentCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    ICurrentUserService currentUserService) : IRequestHandler<UploadAttachmentCommand, Guid>
{
    public async Task<Guid> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        var blobName = $"{request.ProjectId}/{request.TicketId}/{Guid.NewGuid()}/{request.FileName}";
        var blobUrl = await blobStorageService.UploadAsync(
            "ticket-attachments",
            blobName,
            request.FileStream,
            request.ContentType,
            cancellationToken);

        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            FileName = request.FileName,
            BlobUrl = blobUrl,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            UploadedById = currentUserService.UserId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TicketAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return attachment.Id;
    }
}

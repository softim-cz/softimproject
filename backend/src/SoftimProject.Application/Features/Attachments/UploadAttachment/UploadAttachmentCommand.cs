using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Attachments.GetAttachments;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Attachments.UploadAttachment;

public sealed record UploadAttachmentCommand(
    Guid ProjectId,
    Guid TicketId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream) : IRequest<AttachmentDto>, IRequireProjectAccess;

public sealed class UploadAttachmentCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorageService,
    ICurrentUserService currentUserService) : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        await dbContext.GetTicketForProjectAsync(request.ProjectId, request.TicketId, cancellationToken);

        var uploadedById = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

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
            UploadedById = uploadedById,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TicketAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.TicketAttachments
            .AsNoTracking()
            .Where(a => a.Id == attachment.Id)
            .Select(a => new AttachmentDto(
                a.Id,
                a.TicketId,
                a.FileName,
                a.BlobUrl,
                a.ContentType,
                a.FileSizeBytes,
                a.UploadedById,
                a.UploadedBy.DisplayName,
                a.CreatedAt))
            .SingleAsync(cancellationToken);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Attachments.GetAttachments;

public sealed record AttachmentDto(
    Guid Id,
    Guid TicketId,
    string FileName,
    string BlobUrl,
    string ContentType,
    long FileSizeBytes,
    string UploadedByName,
    DateTime CreatedAt);

public sealed record GetAttachmentsQuery(Guid ProjectId, Guid TicketId) : IRequest<List<AttachmentDto>>, IRequireProjectAccess;

public sealed class GetAttachmentsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAttachmentsQuery, List<AttachmentDto>>
{
    public async Task<List<AttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TicketAttachments
            .Where(a => a.TicketId == request.TicketId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentDto(
                a.Id, a.TicketId, a.FileName, a.BlobUrl, a.ContentType,
                a.FileSizeBytes, a.UploadedBy.DisplayName, a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

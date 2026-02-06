using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Comments.GetComments;

public sealed record CommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Content,
    bool IsInternal,
    CommentSource Source,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record GetCommentsQuery(Guid ProjectId, Guid TicketId) : IRequest<List<CommentDto>>, IRequireProjectAccess;

public sealed class GetCommentsQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetCommentsQuery, List<CommentDto>>
{
    public async Task<List<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Comments
            .Where(c => c.TicketId == request.TicketId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id,
                c.AuthorId,
                c.Author.DisplayName,
                c.Author.AvatarUrl,
                c.Content,
                c.IsInternal,
                c.Source,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}

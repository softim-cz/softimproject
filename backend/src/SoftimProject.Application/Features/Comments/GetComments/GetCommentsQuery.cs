using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Comments.GetComments;

public sealed record CommentAuthorDto(
    Guid Id,
    string DisplayName,
    string? AvatarUrl);

public sealed record CommentDto(
    Guid Id,
    CommentAuthorDto Author,
    string Content,
    bool IsInternal,
    CommentSource Source,
    string? ExternalUser,
    Guid? TicketId,
    Guid? ProjectId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record GetCommentsQuery(
    Guid ProjectId,
    Guid TicketId,
    int Page = 1,
    int PageSize = 100) : IRequest<PagedResult<CommentDto>>, IRequireProjectAccess;

public sealed class GetCommentsQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetCommentsQuery, PagedResult<CommentDto>>
{
    public async Task<PagedResult<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Comments
            .AsNoTracking()
            .Where(c => c.TicketId == request.TicketId && c.Ticket != null && c.Ticket.ProjectId == request.ProjectId)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommentDto(
                c.Id,
                new CommentAuthorDto(c.AuthorId, c.Author.DisplayName, c.Author.AvatarUrl),
                c.Content,
                c.IsInternal,
                c.Source,
                c.ExternalUser,
                c.TicketId,
                c.ProjectId,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentDto>(items, totalCount, page, pageSize);
    }
}

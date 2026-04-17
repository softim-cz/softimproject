using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Comments.DeleteComment;

public sealed record DeleteCommentCommand(
    Guid ProjectId,
    Guid TicketId,
    Guid CommentId) : IRequest, IRequireProjectAccess;

public sealed class DeleteCommentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteCommentCommand>
{
    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await dbContext.GetTicketCommentForProjectAsync(
            request.ProjectId,
            request.TicketId,
            request.CommentId,
            cancellationToken);

        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        if (comment.AuthorId != userId
            && !currentUserService.IsInRole("Admin")
            && !currentUserService.IsInRole("Manager"))
        {
            throw new UnauthorizedAccessException("Only the author, Admin or Manager can delete this comment.");
        }

        dbContext.Comments.Remove(comment);

        if (comment.Ticket is not null)
        {
            var latestComment = await dbContext.Comments
                .Where(c => c.TicketId == request.TicketId && c.Id != request.CommentId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.Content)
                .FirstOrDefaultAsync(cancellationToken);

            comment.Ticket.LastComment = latestComment?.Length > 2000
                ? latestComment[..2000]
                : latestComment;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

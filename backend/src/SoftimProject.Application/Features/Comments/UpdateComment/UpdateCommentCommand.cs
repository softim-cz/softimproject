using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Comments.UpdateComment;

public sealed record UpdateCommentCommand(
    Guid ProjectId,
    Guid TicketId,
    Guid CommentId,
    string Content,
    bool? IsInternal = null) : IRequest, IRequireProjectAccess;

public sealed class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public sealed class UpdateCommentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateCommentCommand>
{
    public async Task Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await dbContext.GetTicketCommentForProjectAsync(
            request.ProjectId,
            request.TicketId,
            request.CommentId,
            cancellationToken);

        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        if (comment.AuthorId != userId && !currentUserService.IsInRole("Admin"))
        {
            var isProjectManager = await dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == request.ProjectId
                    && pm.UserId == userId
                    && pm.Role == ProjectRole.ProjectManager, cancellationToken);
            if (!isProjectManager)
                throw new UnauthorizedAccessException("Only the author, the project manager, or Admin can edit this comment.");
        }

        comment.Content = request.Content;
        if (request.IsInternal.HasValue)
            comment.IsInternal = request.IsInternal.Value;
        comment.UpdatedAt = DateTime.UtcNow;

        if (comment.Ticket is not null)
        {
            var latestComment = await dbContext.Comments
                .Where(c => c.TicketId == request.TicketId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Id, c.Content })
                .FirstOrDefaultAsync(cancellationToken);

            var latestContent = latestComment is null
                ? null
                : latestComment.Id == request.CommentId
                    ? request.Content
                    : latestComment.Content;

            comment.Ticket.LastComment = latestContent?.Length > 2000
                ? latestContent[..2000]
                : latestContent;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

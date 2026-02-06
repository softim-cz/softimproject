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
    IApplicationDbContext dbContext) : IRequestHandler<DeleteCommentCommand>
{
    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && c.TicketId == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Comment), request.CommentId);

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

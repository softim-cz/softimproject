using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Comments.UpdateComment;

public sealed record UpdateCommentCommand(
    Guid ProjectId,
    Guid TicketId,
    Guid CommentId,
    string Content) : IRequest, IRequireProjectAccess;

public sealed class UpdateCommentCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateCommentCommand>
{
    public async Task Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && c.TicketId == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Comment), request.CommentId);

        comment.Content = request.Content;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

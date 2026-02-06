using FluentValidation;
using MediatR;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Comments.CreateComment;

public sealed record CreateCommentCommand(
    Guid ProjectId,
    Guid TicketId,
    string Content,
    bool IsInternal) : IRequest<Guid>, IRequireProjectAccess;

public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public sealed class CreateCommentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateCommentCommand, Guid>
{
    public async Task<Guid> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            AuthorId = currentUserService.UserId ?? Guid.Empty,
            Content = request.Content,
            IsInternal = request.IsInternal,
            Source = CommentSource.Manual,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }
}

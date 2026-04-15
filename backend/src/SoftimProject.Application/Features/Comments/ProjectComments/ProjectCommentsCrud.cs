using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Comments.GetComments;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Comments.ProjectComments;

public sealed record GetProjectCommentsQuery(Guid ProjectId) : IRequest<List<CommentDto>>, IRequireProjectAccess;

public sealed class GetProjectCommentsQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetProjectCommentsQuery, List<CommentDto>>
{
    public async Task<List<CommentDto>> Handle(GetProjectCommentsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Comments
            .AsNoTracking()
            .Where(c => c.ProjectId == request.ProjectId && c.TicketId == null)
            .OrderByDescending(c => c.CreatedAt)
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
    }
}

public sealed record CreateProjectCommentCommand(
    Guid ProjectId,
    string Content,
    bool IsInternal) : IRequest<Guid>, IRequireProjectAccess;

public sealed class CreateProjectCommentCommandValidator : AbstractValidator<CreateProjectCommentCommand>
{
    public CreateProjectCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public sealed class CreateProjectCommentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateProjectCommentCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommentCommand request, CancellationToken cancellationToken)
    {
        var authorId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            TicketId = null,
            AuthorId = authorId,
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

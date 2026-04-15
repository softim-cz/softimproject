using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Kanban.CreateColumn;

public sealed record CreateColumnCommand(
    Guid ProjectId,
    Guid BoardId,
    string Name,
    int? WipLimit,
    List<Guid> MapsToTaskStateIds,
    string? Color) : IRequest<Guid>, IRequireProjectAccess;

public sealed class CreateColumnCommandValidator : AbstractValidator<CreateColumnCommand>
{
    public CreateColumnCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WipLimit).GreaterThan(0).When(x => x.WipLimit.HasValue);
        RuleFor(x => x.MapsToTaskStateIds).NotEmpty();
        RuleForEach(x => x.MapsToTaskStateIds).NotEmpty();
    }
}

public sealed class CreateColumnCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<CreateColumnCommand, Guid>
{
    public async Task<Guid> Handle(CreateColumnCommand request, CancellationToken cancellationToken)
    {
        var maxPosition = await dbContext.KanbanColumns
            .Where(c => c.BoardId == request.BoardId)
            .Select(c => (int?)c.Position)
            .MaxAsync(cancellationToken) ?? -1;

        var taskStates = await dbContext.TaskStates
            .Where(ts => request.MapsToTaskStateIds.Contains(ts.Id))
            .ToListAsync(cancellationToken);

        var column = new KanbanColumn
        {
            Id = Guid.NewGuid(),
            BoardId = request.BoardId,
            Name = request.Name,
            Position = maxPosition + 1,
            WipLimit = request.WipLimit,
            Color = request.Color,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var ts in taskStates)
            column.MapsToTaskStates.Add(ts);

        dbContext.KanbanColumns.Add(column);
        await dbContext.SaveChangesAsync(cancellationToken);

        return column.Id;
    }
}

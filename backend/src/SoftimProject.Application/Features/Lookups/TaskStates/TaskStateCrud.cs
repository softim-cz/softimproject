using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.TaskStates;

// DTO
public sealed record TaskStateDto(Guid Id, string Name, string Color, int SortOrder, bool IsActive, bool IsDefault, bool IsClosedState);

// GET ALL
public sealed record GetTaskStatesQuery : IRequest<List<TaskStateDto>>;

public sealed class GetTaskStatesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTaskStatesQuery, List<TaskStateDto>>
{
    public async Task<List<TaskStateDto>> Handle(GetTaskStatesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskStates
            .OrderBy(ts => ts.SortOrder).ThenBy(ts => ts.Name)
            .Select(ts => new TaskStateDto(ts.Id, ts.Name, ts.Color, ts.SortOrder, ts.IsActive, ts.IsDefault, ts.IsClosedState))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateTaskStateCommand(string Name, string Color, int SortOrder, bool IsDefault, bool IsClosedState) : IRequest<Guid>;

public sealed class CreateTaskStateCommandValidator : AbstractValidator<CreateTaskStateCommand>
{
    public CreateTaskStateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateTaskStateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateTaskStateCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskStateCommand request, CancellationToken cancellationToken)
    {
        var entity = new TaskState
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Color = request.Color,
            SortOrder = request.SortOrder,
            IsActive = true,
            IsDefault = request.IsDefault,
            IsClosedState = request.IsClosedState,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TaskStates.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// UPDATE
public sealed record UpdateTaskStateCommand(Guid Id, string Name, string Color, int SortOrder, bool IsActive, bool IsDefault, bool IsClosedState) : IRequest;

public sealed class UpdateTaskStateCommandValidator : AbstractValidator<UpdateTaskStateCommand>
{
    public UpdateTaskStateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);
    }
}

public sealed class UpdateTaskStateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateTaskStateCommand>
{
    public async Task Handle(UpdateTaskStateCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaskStates
            .FirstOrDefaultAsync(ts => ts.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskState), request.Id);

        entity.Name = request.Name;
        entity.Color = request.Color;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.IsDefault = request.IsDefault;
        entity.IsClosedState = request.IsClosedState;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteTaskStateCommand(Guid Id) : IRequest;

public sealed class DeleteTaskStateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteTaskStateCommand>
{
    public async Task Handle(DeleteTaskStateCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaskStates
            .FirstOrDefaultAsync(ts => ts.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskState), request.Id);

        dbContext.TaskStates.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.TaskTypes;

// DTO
public sealed record TaskTypeDto(Guid Id, string Name, string? Icon, int SortOrder, bool IsActive);

// GET ALL
public sealed record GetTaskTypesQuery : IRequest<List<TaskTypeDto>>;

public sealed class GetTaskTypesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTaskTypesQuery, List<TaskTypeDto>>
{
    public async Task<List<TaskTypeDto>> Handle(GetTaskTypesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskTypes
            .OrderBy(tt => tt.SortOrder).ThenBy(tt => tt.Name)
            .Select(tt => new TaskTypeDto(tt.Id, tt.Name, tt.Icon, tt.SortOrder, tt.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateTaskTypeCommand(string Name, string? Icon, int SortOrder) : IRequest<Guid>;

public sealed class CreateTaskTypeCommandValidator : AbstractValidator<CreateTaskTypeCommand>
{
    public CreateTaskTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Icon).MaximumLength(100);
    }
}

public sealed class CreateTaskTypeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateTaskTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new TaskType
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TaskTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// UPDATE
public sealed record UpdateTaskTypeCommand(Guid Id, string Name, string? Icon, int SortOrder, bool IsActive) : IRequest;

public sealed class UpdateTaskTypeCommandValidator : AbstractValidator<UpdateTaskTypeCommand>
{
    public UpdateTaskTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Icon).MaximumLength(100);
    }
}

public sealed class UpdateTaskTypeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateTaskTypeCommand>
{
    public async Task Handle(UpdateTaskTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaskTypes
            .FirstOrDefaultAsync(tt => tt.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskType), request.Id);

        entity.Name = request.Name;
        entity.Icon = request.Icon;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteTaskTypeCommand(Guid Id) : IRequest;

public sealed class DeleteTaskTypeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteTaskTypeCommand>
{
    public async Task Handle(DeleteTaskTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaskTypes
            .FirstOrDefaultAsync(tt => tt.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskType), request.Id);

        dbContext.TaskTypes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

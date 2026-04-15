using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.ProjectTypes;

// DTO
public sealed record ProjectTypeDto(Guid Id, string Name, string? Description, int SortOrder, bool IsActive);

// GET ALL
public sealed record GetProjectTypesQuery : IRequest<List<ProjectTypeDto>>;

public sealed class GetProjectTypesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProjectTypesQuery, List<ProjectTypeDto>>
{
    public async Task<List<ProjectTypeDto>> Handle(GetProjectTypesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectTypes
            .OrderBy(pt => pt.SortOrder).ThenBy(pt => pt.Name)
            .Select(pt => new ProjectTypeDto(pt.Id, pt.Name, pt.Description, pt.SortOrder, pt.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateProjectTypeCommand(string Name, string? Description, int SortOrder) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class CreateProjectTypeCommandValidator : AbstractValidator<CreateProjectTypeCommand>
{
    public CreateProjectTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateProjectTypeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateProjectTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new ProjectType
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ProjectTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// UPDATE
public sealed record UpdateProjectTypeCommand(Guid Id, string Name, string? Description, int SortOrder, bool IsActive) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateProjectTypeCommandValidator : AbstractValidator<UpdateProjectTypeCommand>
{
    public UpdateProjectTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateProjectTypeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateProjectTypeCommand>
{
    public async Task Handle(UpdateProjectTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ProjectTypes
            .FirstOrDefaultAsync(pt => pt.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectType), request.Id);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteProjectTypeCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DeleteProjectTypeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteProjectTypeCommand>
{
    public async Task Handle(DeleteProjectTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ProjectTypes
            .FirstOrDefaultAsync(pt => pt.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectType), request.Id);

        dbContext.ProjectTypes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

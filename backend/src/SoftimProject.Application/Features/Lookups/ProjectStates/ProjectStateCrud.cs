using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.ProjectStates;

// DTO
public sealed record ProjectStateDto(Guid Id, string Name, string? NameCs, string? NameEn, string Color, int SortOrder, bool IsActive, bool IsDefault);

// GET ALL
public sealed record GetProjectStatesQuery : IRequest<List<ProjectStateDto>>;

public sealed class GetProjectStatesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProjectStatesQuery, List<ProjectStateDto>>
{
    public async Task<List<ProjectStateDto>> Handle(GetProjectStatesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectStates
            .OrderBy(ps => ps.SortOrder).ThenBy(ps => ps.Name)
            .Select(ps => new ProjectStateDto(ps.Id, ps.Name, ps.NameCs, ps.NameEn, ps.Color, ps.SortOrder, ps.IsActive, ps.IsDefault))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateProjectStateCommand(string Name, string? NameCs, string? NameEn, string Color, int SortOrder, bool IsDefault) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class CreateProjectStateCommandValidator : AbstractValidator<CreateProjectStateCommand>
{
    public CreateProjectStateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateProjectStateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateProjectStateCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectStateCommand request, CancellationToken cancellationToken)
    {
        var entity = new ProjectState
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            NameCs = request.NameCs,
            NameEn = request.NameEn,
            Color = request.Color,
            SortOrder = request.SortOrder,
            IsActive = true,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ProjectStates.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// UPDATE
public sealed record UpdateProjectStateCommand(Guid Id, string Name, string? NameCs, string? NameEn, string Color, int SortOrder, bool IsActive, bool IsDefault) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateProjectStateCommandValidator : AbstractValidator<UpdateProjectStateCommand>
{
    public UpdateProjectStateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);
    }
}

public sealed class UpdateProjectStateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateProjectStateCommand>
{
    public async Task Handle(UpdateProjectStateCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ProjectStates
            .FirstOrDefaultAsync(ps => ps.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectState), request.Id);

        entity.Name = request.Name;
        entity.NameCs = request.NameCs;
        entity.NameEn = request.NameEn;
        entity.Color = request.Color;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.IsDefault = request.IsDefault;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteProjectStateCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DeleteProjectStateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteProjectStateCommand>
{
    public async Task Handle(DeleteProjectStateCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ProjectStates
            .FirstOrDefaultAsync(ps => ps.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectState), request.Id);

        dbContext.ProjectStates.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

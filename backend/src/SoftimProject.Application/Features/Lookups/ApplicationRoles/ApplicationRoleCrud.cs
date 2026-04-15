using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.ApplicationRoles;

// DTO
public sealed record ApplicationRoleDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool ProjectsCreate,
    bool ProjectsRead,
    bool ProjectsUpdate,
    bool ProjectsDelete,
    bool TimeTrackingCreate,
    bool TimeTrackingRead,
    bool TimeTrackingUpdate,
    bool TimeTrackingDelete,
    bool ReportsCreate,
    bool ReportsRead,
    bool ReportsUpdate,
    bool ReportsDelete);

// GET ALL
public sealed record GetApplicationRolesQuery : IRequest<List<ApplicationRoleDto>>;

public sealed class GetApplicationRolesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetApplicationRolesQuery, List<ApplicationRoleDto>>
{
    public async Task<List<ApplicationRoleDto>> Handle(GetApplicationRolesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.ApplicationRoles
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .Select(r => new ApplicationRoleDto(
                r.Id, r.Name, r.Description, r.SortOrder,
                r.ProjectsCreate, r.ProjectsRead, r.ProjectsUpdate, r.ProjectsDelete,
                r.TimeTrackingCreate, r.TimeTrackingRead, r.TimeTrackingUpdate, r.TimeTrackingDelete,
                r.ReportsCreate, r.ReportsRead, r.ReportsUpdate, r.ReportsDelete))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateApplicationRoleCommand(
    string Name,
    string? Description,
    int SortOrder,
    bool ProjectsCreate,
    bool ProjectsRead,
    bool ProjectsUpdate,
    bool ProjectsDelete,
    bool TimeTrackingCreate,
    bool TimeTrackingRead,
    bool TimeTrackingUpdate,
    bool TimeTrackingDelete,
    bool ReportsCreate,
    bool ReportsRead,
    bool ReportsUpdate,
    bool ReportsDelete) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class CreateApplicationRoleCommandValidator : AbstractValidator<CreateApplicationRoleCommand>
{
    public CreateApplicationRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateApplicationRoleCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateApplicationRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateApplicationRoleCommand request, CancellationToken cancellationToken)
    {
        var entity = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            ProjectsCreate = request.ProjectsCreate,
            ProjectsRead = request.ProjectsRead,
            ProjectsUpdate = request.ProjectsUpdate,
            ProjectsDelete = request.ProjectsDelete,
            TimeTrackingCreate = request.TimeTrackingCreate,
            TimeTrackingRead = request.TimeTrackingRead,
            TimeTrackingUpdate = request.TimeTrackingUpdate,
            TimeTrackingDelete = request.TimeTrackingDelete,
            ReportsCreate = request.ReportsCreate,
            ReportsRead = request.ReportsRead,
            ReportsUpdate = request.ReportsUpdate,
            ReportsDelete = request.ReportsDelete,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ApplicationRoles.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// UPDATE
public sealed record UpdateApplicationRoleCommand(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool ProjectsCreate,
    bool ProjectsRead,
    bool ProjectsUpdate,
    bool ProjectsDelete,
    bool TimeTrackingCreate,
    bool TimeTrackingRead,
    bool TimeTrackingUpdate,
    bool TimeTrackingDelete,
    bool ReportsCreate,
    bool ReportsRead,
    bool ReportsUpdate,
    bool ReportsDelete) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateApplicationRoleCommandValidator : AbstractValidator<UpdateApplicationRoleCommand>
{
    public UpdateApplicationRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateApplicationRoleCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateApplicationRoleCommand>
{
    public async Task Handle(UpdateApplicationRoleCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApplicationRoles
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRole), request.Id);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.SortOrder = request.SortOrder;
        entity.ProjectsCreate = request.ProjectsCreate;
        entity.ProjectsRead = request.ProjectsRead;
        entity.ProjectsUpdate = request.ProjectsUpdate;
        entity.ProjectsDelete = request.ProjectsDelete;
        entity.TimeTrackingCreate = request.TimeTrackingCreate;
        entity.TimeTrackingRead = request.TimeTrackingRead;
        entity.TimeTrackingUpdate = request.TimeTrackingUpdate;
        entity.TimeTrackingDelete = request.TimeTrackingDelete;
        entity.ReportsCreate = request.ReportsCreate;
        entity.ReportsRead = request.ReportsRead;
        entity.ReportsUpdate = request.ReportsUpdate;
        entity.ReportsDelete = request.ReportsDelete;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteApplicationRoleCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DeleteApplicationRoleCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteApplicationRoleCommand>
{
    public async Task Handle(DeleteApplicationRoleCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApplicationRoles
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRole), request.Id);

        dbContext.ApplicationRoles.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

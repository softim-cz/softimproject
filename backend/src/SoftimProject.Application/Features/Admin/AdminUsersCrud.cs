using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Admin;

// DTO
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    GlobalRole GlobalRole,
    bool IsActive,
    string? FirstName,
    string? LastName,
    string? CorporateRole,
    string? CompanyName,
    List<Guid> ApplicationRoleIds);

// GET ALL USERS
public sealed record GetAdminUsersQuery : IRequest<List<AdminUserDto>>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class GetAdminUsersQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAdminUsersQuery, List<AdminUserDto>>
{
    public async Task<List<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Select(u => new AdminUserDto(
                u.Id,
                u.Email,
                u.DisplayName,
                u.AvatarUrl,
                u.GlobalRole,
                u.IsActive,
                u.FirstName,
                u.LastName,
                u.CorporateRole,
                u.CompanyName,
                u.UserApplicationRoles.Select(uar => uar.ApplicationRoleId).ToList()))
            .ToListAsync(cancellationToken);
    }
}

// UPDATE USER ROLES
public sealed record UpdateUserRolesCommand(Guid UserId, List<Guid> ApplicationRoleIds) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateUserRolesCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateUserRolesCommand>
{
    public async Task Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        // Remove existing roles
        var existing = await dbContext.UserApplicationRoles
            .Where(uar => uar.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        foreach (var uar in existing)
            dbContext.UserApplicationRoles.Remove(uar);

        // Add new roles
        foreach (var roleId in request.ApplicationRoleIds)
        {
            dbContext.UserApplicationRoles.Add(new UserApplicationRole
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                ApplicationRoleId = roleId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using FluentValidation;
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

// UPDATE GLOBAL ROLE
public sealed record UpdateUserGlobalRoleCommand(Guid UserId, GlobalRole GlobalRole) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateUserGlobalRoleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateUserGlobalRoleCommand>
{
    public async Task Handle(UpdateUserGlobalRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.GlobalRole == request.GlobalRole)
            return;

        // Self-demote and last-admin demote would lock the system out of admin-only operations.
        var demotingFromAdmin = user.GlobalRole == GlobalRole.Admin && request.GlobalRole != GlobalRole.Admin;
        if (demotingFromAdmin)
        {
            if (currentUserService.UserId == request.UserId)
                throw new ValidationException("Admins cannot demote their own account.");

            var otherAdmins = await dbContext.Users
                .CountAsync(u => u.Id != request.UserId && u.GlobalRole == GlobalRole.Admin && u.IsActive, cancellationToken);
            if (otherAdmins == 0)
                throw new ValidationException("Cannot demote the last active Admin.");
        }

        user.GlobalRole = request.GlobalRole;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// UPDATE ACTIVE STATUS
public sealed record UpdateUserActiveCommand(Guid UserId, bool IsActive) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateUserActiveCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateUserActiveCommand>
{
    public async Task Handle(UpdateUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.IsActive == request.IsActive)
            return;

        if (!request.IsActive)
        {
            if (currentUserService.UserId == request.UserId)
                throw new ValidationException("Admins cannot deactivate their own account.");

            if (user.GlobalRole == GlobalRole.Admin)
            {
                var otherActiveAdmins = await dbContext.Users
                    .CountAsync(u => u.Id != request.UserId && u.GlobalRole == GlobalRole.Admin && u.IsActive, cancellationToken);
                if (otherActiveAdmins == 0)
                    throw new ValidationException("Cannot deactivate the last active Admin.");
            }
        }

        user.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

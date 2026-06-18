using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserDirectory _userDirectory;
    private Guid? _userId;
    private GlobalRole? _globalRole;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IApplicationDbContext dbContext,
        IUserDirectory userDirectory)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
        _userDirectory = userDirectory;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? EntraObjectId => User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? User?.FindFirst("oid")?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("preferred_username")?.Value;

    public string? DisplayName => User?.FindFirst("name")?.Value;

    public string? FirstName => User?.FindFirst(ClaimTypes.GivenName)?.Value
        ?? User?.FindFirst("given_name")?.Value;

    public string? LastName => User?.FindFirst(ClaimTypes.Surname)?.Value
        ?? User?.FindFirst("family_name")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId => _userId;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_userId.HasValue || EntraObjectId is null) return;

        var existing = await _dbContext.Users
            .Where(u => u.EntraObjectId == EntraObjectId)
            .Select(u => new { u.Id, u.GlobalRole })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            _userId = existing.Id;
            _globalRole = existing.GlobalRole;
            return;
        }

        // Auto-provision user on first login
        var hasAnyUsers = await _dbContext.Users.AnyAsync(cancellationToken);

        // Firemní role a firma nejsou v přihlašovacím tokenu — doplníme je z Graphu
        // (jen jednou, při prvním přihlášení). Při nedostupnosti zůstanou prázdné.
        var profile = await _userDirectory.GetProfileAsync(EntraObjectId, cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = EntraObjectId,
            Email = Email ?? "unknown",
            DisplayName = DisplayName ?? Email ?? "Unknown User",
            FirstName = FirstName,
            LastName = LastName,
            CorporateRole = profile?.JobTitle,
            CompanyName = profile?.CompanyName,
            GlobalRole = hasAnyUsers ? GlobalRole.User : GlobalRole.Admin, // First user becomes Admin
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _userId = user.Id;
        _globalRole = user.GlobalRole;
    }

    public bool IsInRole(string role) =>
        (Enum.TryParse<GlobalRole>(role, out var parsed) && _globalRole == parsed)
        || (User?.IsInRole(role) ?? false);

    public async Task<bool> HasProjectAccessAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!UserId.HasValue) return false;
        if (IsInRole("Admin")) return true;
        return await _dbContext.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == UserId.Value, cancellationToken);
    }
}

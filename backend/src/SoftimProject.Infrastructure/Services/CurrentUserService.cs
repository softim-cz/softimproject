using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationDbContext _dbContext;
    private Guid? _userId;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IApplicationDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? EntraObjectId => User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? User?.FindFirst("oid")?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("preferred_username")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            if (_userId.HasValue) return _userId;
            if (EntraObjectId is null) return null;
            _userId = _dbContext.Users
                .Where(u => u.EntraObjectId == EntraObjectId)
                .Select(u => u.Id)
                .FirstOrDefault();
            return _userId == Guid.Empty ? null : _userId;
        }
    }

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public async Task<bool> HasProjectAccessAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!UserId.HasValue) return false;
        if (IsInRole("Admin")) return true;
        return await _dbContext.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == UserId.Value, cancellationToken);
    }
}

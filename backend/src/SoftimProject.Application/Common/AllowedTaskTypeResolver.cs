using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Common;

/// <summary>
/// Resolves which TaskTypes are allowed for a given project, applying the
/// "default on template, override per project" rule:
/// <list type="bullet">
/// <item>If the project defines its own allow-list, that set wins (override).</item>
/// <item>Otherwise the project's template allow-list applies (default).</item>
/// <item>If neither is configured the project is <em>unrestricted</em> — any active
/// TaskType may be used, preserving the behaviour from before this feature.</item>
/// </list>
/// </summary>
public static class AllowedTaskTypeResolver
{
    /// <summary>
    /// Returns the set of allowed TaskType ids for the project, or <c>null</c> when the
    /// project is unrestricted (no allow-list configured on the project or its template).
    /// </summary>
    public static async Task<HashSet<Guid>?> GetEffectiveAllowedTaskTypeIdsAsync(
        IApplicationDbContext dbContext, Guid projectId, CancellationToken cancellationToken)
    {
        var projectOverride = await dbContext.Projects
            .Where(p => p.Id == projectId)
            .SelectMany(p => p.AllowedTaskTypes.Select(tt => tt.Id))
            .ToListAsync(cancellationToken);

        if (projectOverride.Count > 0)
            return projectOverride.ToHashSet();

        var templateDefault = await dbContext.Projects
            .Where(p => p.Id == projectId)
            .SelectMany(p => p.ProjectTemplate.AllowedTaskTypes.Select(tt => tt.Id))
            .ToListAsync(cancellationToken);

        if (templateDefault.Count > 0)
            return templateDefault.ToHashSet();

        return null;
    }

    /// <summary>
    /// Throws <see cref="ValidationException"/> when the chosen TaskType is not permitted
    /// for the project. No-op when no TaskType is chosen or the project is unrestricted.
    /// </summary>
    public static async Task ValidateTaskTypeAsync(
        IApplicationDbContext dbContext, Guid projectId, Guid? taskTypeId, CancellationToken cancellationToken)
    {
        if (!taskTypeId.HasValue)
            return;

        var allowed = await GetEffectiveAllowedTaskTypeIdsAsync(dbContext, projectId, cancellationToken);
        if (allowed is not null && !allowed.Contains(taskTypeId.Value))
            throw new ValidationException("Zvolený typ úkolu není pro tento projekt povolen.");
    }
}

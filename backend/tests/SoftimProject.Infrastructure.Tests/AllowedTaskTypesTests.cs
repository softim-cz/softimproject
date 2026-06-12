using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class AllowedTaskTypesTests
{
    [Fact]
    public async Task Unrestricted_When_Neither_Project_Nor_Template_Configures_Allowlist()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);

        var effective = await AllowedTaskTypeResolver.GetEffectiveAllowedTaskTypeIdsAsync(db, seed.Project.Id, CancellationToken.None);
        effective.Should().BeNull();

        // Any task type — and null — must be accepted.
        var act = () => AllowedTaskTypeResolver.ValidateTaskTypeAsync(db, seed.Project.Id, seed.TypeBug.Id, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Template_Allowlist_Applies_When_Project_Has_No_Override()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);

        seed.Template.AllowedTaskTypes.Add(seed.TypeBug);
        await db.SaveChangesAsync();

        var effective = await AllowedTaskTypeResolver.GetEffectiveAllowedTaskTypeIdsAsync(db, seed.Project.Id, CancellationToken.None);
        effective.Should().BeEquivalentTo(new[] { seed.TypeBug.Id });

        var ok = () => AllowedTaskTypeResolver.ValidateTaskTypeAsync(db, seed.Project.Id, seed.TypeBug.Id, CancellationToken.None);
        await ok.Should().NotThrowAsync();

        var rejected = () => AllowedTaskTypeResolver.ValidateTaskTypeAsync(db, seed.Project.Id, seed.TypeFeature.Id, CancellationToken.None);
        await rejected.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Project_Override_Replaces_Template_Default()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);

        // Template allows only Bug; project overrides to only Feature.
        seed.Template.AllowedTaskTypes.Add(seed.TypeBug);
        seed.Project.AllowedTaskTypes.Add(seed.TypeFeature);
        await db.SaveChangesAsync();

        var effective = await AllowedTaskTypeResolver.GetEffectiveAllowedTaskTypeIdsAsync(db, seed.Project.Id, CancellationToken.None);
        effective.Should().BeEquivalentTo(new[] { seed.TypeFeature.Id });

        var ok = () => AllowedTaskTypeResolver.ValidateTaskTypeAsync(db, seed.Project.Id, seed.TypeFeature.Id, CancellationToken.None);
        await ok.Should().NotThrowAsync();

        // Bug is allowed by the template but the project override removed it.
        var rejected = () => AllowedTaskTypeResolver.ValidateTaskTypeAsync(db, seed.Project.Id, seed.TypeBug.Id, CancellationToken.None);
        await rejected.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Null_TaskType_Is_Always_Accepted_Even_When_Restricted()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);

        seed.Template.AllowedTaskTypes.Add(seed.TypeBug);
        await db.SaveChangesAsync();

        var act = () => AllowedTaskTypeResolver.ValidateTaskTypeAsync(db, seed.Project.Id, null, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(ProjectTemplate Template, Project Project, TaskType TypeBug, TaskType TypeFeature)> SeedAsync(ApplicationDbContext db)
    {
        var template = new ProjectTemplate
        {
            Id = Guid.NewGuid(),
            Name = $"Template-{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Main project",
            Code = "MAIN",
            Status = ProjectStatus.Active,
            ProjectTemplateId = template.Id,
            ProjectTemplate = template,
            CreatedAt = DateTime.UtcNow,
        };

        var typeBug = new TaskType { Id = Guid.NewGuid(), Name = "Bug", SortOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        var typeFeature = new TaskType { Id = Guid.NewGuid(), Name = "Feature", SortOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow };

        db.ProjectTemplates.Add(template);
        db.Projects.Add(project);
        db.TaskTypes.AddRange(typeBug, typeFeature);
        await db.SaveChangesAsync();

        return (template, project, typeBug, typeFeature);
    }
}

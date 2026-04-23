using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

// Extends IRequireProjectAccess with a required ProjectRole in the project. GlobalRole.Admin
// bypasses the role check (but still enforces that the project exists). Use this marker for
// management operations on a project where plain membership is too permissive — editing the
// project metadata, portal tokens, members, kanban board config, GitHub integration.
public interface IRequireProjectRole : IRequireProjectAccess
{
    ProjectRole RequiredProjectRole { get; }
}

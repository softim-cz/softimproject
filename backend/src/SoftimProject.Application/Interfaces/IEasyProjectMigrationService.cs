using SoftimProject.Application.Features.Migration.EasyProject;

namespace SoftimProject.Application.Interfaces;

public interface IEasyProjectMigrationService
{
    Task ExecuteAsync(Guid jobId, StartMigrationCommand command);
}

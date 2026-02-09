namespace SoftimProject.Application.Interfaces;

public enum PermissionArea
{
    Projects,
    TimeTracking,
    Reports
}

public enum PermissionOperation
{
    Create,
    Read,
    Update,
    Delete
}

public interface IRequirePermission
{
    PermissionArea Area { get; }
    PermissionOperation Operation { get; }
}

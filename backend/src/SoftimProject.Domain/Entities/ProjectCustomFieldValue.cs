using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ProjectCustomFieldValue : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid CustomFieldDefinitionId { get; set; }
    public string? Value { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;
}

using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ProjectTemplateField : BaseEntity
{
    public Guid ProjectTemplateId { get; set; }
    public Guid CustomFieldDefinitionId { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public ProjectTemplate ProjectTemplate { get; set; } = null!;
    public CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;
}

using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class CustomFieldDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CustomFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public string? Options { get; set; } // JSON array for Select type, e.g. ["A","B","C"]
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AppliesTo { get; set; } // "Project", "Ticket", or null (both)

    // Navigation properties
    public ICollection<ProjectCustomFieldValue> Values { get; set; } = new List<ProjectCustomFieldValue>();
    public ICollection<TicketCustomFieldValue> TicketValues { get; set; } = new List<TicketCustomFieldValue>();
    public ICollection<ProjectTemplateField> TemplateFields { get; set; } = new List<ProjectTemplateField>();
}

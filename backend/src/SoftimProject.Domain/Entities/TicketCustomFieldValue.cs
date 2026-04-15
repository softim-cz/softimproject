using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class TicketCustomFieldValue : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid CustomFieldDefinitionId { get; set; }
    public string? Value { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;
}

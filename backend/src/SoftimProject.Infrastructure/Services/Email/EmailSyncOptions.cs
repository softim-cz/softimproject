namespace SoftimProject.Infrastructure.Services.Email;

public sealed class EmailSyncOptions
{
    public const string SectionName = "Sync:Email";

    public bool Enabled { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string MailboxUserId { get; set; } = string.Empty;
    public string AliasPrefix { get; set; } = "inbox+";
    public int BatchSize { get; set; } = 50;
}

namespace SoftimProject.Application.Interfaces;

public interface IEmailMailboxClient
{
    Task<IReadOnlyList<EmailMessage>> FetchUnreadAsync(int take, CancellationToken cancellationToken);
    Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken);
}

public sealed record EmailMessage(
    string Id,
    string Subject,
    string Body,
    string FromAddress,
    string? FromDisplayName,
    IReadOnlyList<string> ToRecipients,
    IReadOnlyList<string> CcRecipients,
    DateTimeOffset ReceivedAt);

using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services.Email;

public sealed class GraphMailboxClient : IEmailMailboxClient
{
    private readonly GraphServiceClient _graph;
    private readonly EmailSyncOptions _options;

    public GraphMailboxClient(IOptions<EmailSyncOptions> options)
    {
        _options = options.Value;

        var credential = new ClientSecretCredential(
            _options.TenantId,
            _options.ClientId,
            _options.ClientSecret);
        _graph = new GraphServiceClient(credential, scopes: ["https://graph.microsoft.com/.default"]);
    }

    public async Task<IReadOnlyList<EmailMessage>> FetchUnreadAsync(int take, CancellationToken cancellationToken)
    {
        var response = await _graph.Users[_options.MailboxUserId].Messages
            .GetAsync(req =>
            {
                req.QueryParameters.Filter = "isRead eq false";
                req.QueryParameters.Top = take;
                req.QueryParameters.Orderby = ["receivedDateTime asc"];
                req.QueryParameters.Select = ["id", "subject", "body", "from", "toRecipients", "ccRecipients", "receivedDateTime"];
            }, cancellationToken)
            ?? throw new InvalidOperationException("Graph returned null response for messages query.");

        var messages = response.Value ?? [];
        return messages.Select(Map).ToList();
    }

    public Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken)
        => _graph.Users[_options.MailboxUserId].Messages[messageId]
            .PatchAsync(new Message { IsRead = true }, cancellationToken: cancellationToken);

    private static EmailMessage Map(Message msg) => new(
        Id: msg.Id ?? string.Empty,
        Subject: msg.Subject ?? string.Empty,
        Body: msg.Body?.Content ?? string.Empty,
        FromAddress: msg.From?.EmailAddress?.Address ?? string.Empty,
        FromDisplayName: msg.From?.EmailAddress?.Name,
        ToRecipients: (msg.ToRecipients ?? []).Select(r => r.EmailAddress?.Address ?? string.Empty).Where(a => a.Length > 0).ToList(),
        CcRecipients: (msg.CcRecipients ?? []).Select(r => r.EmailAddress?.Address ?? string.Empty).Where(a => a.Length > 0).ToList(),
        ReceivedAt: msg.ReceivedDateTime ?? DateTimeOffset.UtcNow);
}

namespace SoftimProject.Application.Interfaces;

public interface IAiService
{
    Task<(string Summary, int TokensUsed)> SummarizeTicketAsync(string title, string description, IEnumerable<string> comments, CancellationToken cancellationToken = default);
    Task<(string Report, int TokensUsed)> GenerateReportAsync(string projectName, string reportType, string periodDescription, string data, CancellationToken cancellationToken = default);
    Task<string?> SuggestStatusTransitionAsync(string ticketTitle, string currentStatus, string latestComment, CancellationToken cancellationToken = default);
}

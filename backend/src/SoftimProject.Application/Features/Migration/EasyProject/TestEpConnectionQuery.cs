using MediatR;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record TestEpConnectionResult(bool Success, string? Error);

// ConnectionId — when a saved connection is picked in the wizard, its URL + stored token are
// used (the user doesn't re-enter the API key).
public sealed record TestEpConnectionQuery(string? BaseUrl, string? ApiKey, Guid? ConnectionId = null) : IRequest<TestEpConnectionResult>;

public sealed class TestEpConnectionQueryHandler(
    IEasyProjectApiClient apiClient,
    IMigrationCredentialResolver credentials) : IRequestHandler<TestEpConnectionQuery, TestEpConnectionResult>
{
    public async Task<TestEpConnectionResult> Handle(TestEpConnectionQuery request, CancellationToken cancellationToken)
    {
        var (baseUrl, apiKey) = await credentials.ResolveAsync(request.BaseUrl, request.ApiKey, request.ConnectionId, cancellationToken);
        var (success, error) = await apiClient.TestConnectionAsync(baseUrl, apiKey, cancellationToken);
        return new TestEpConnectionResult(success, error);
    }
}

using MediatR;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record TestEpConnectionResult(bool Success, string? Error);

public sealed record TestEpConnectionQuery(string BaseUrl, string ApiKey) : IRequest<TestEpConnectionResult>;

public sealed class TestEpConnectionQueryHandler(
    IEasyProjectApiClient apiClient) : IRequestHandler<TestEpConnectionQuery, TestEpConnectionResult>
{
    public async Task<TestEpConnectionResult> Handle(TestEpConnectionQuery request, CancellationToken cancellationToken)
    {
        var (success, error) = await apiClient.TestConnectionAsync(request.BaseUrl, request.ApiKey, cancellationToken);
        return new TestEpConnectionResult(success, error);
    }
}

using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

// Wraps every call to IAiService with an audit row. The caller passes an
// AiInvocationContext (trigger, scope ids, reason) and a lambda that returns
// (output, tokens). The recorder hashes the input, times the call, writes an
// AiInvocation row on the way out — regardless of success/failure.
//
// Rate-limit is enforced before the lambda runs: if the caller's user has made
// more than the configured limit in the recent window, RateLimitExceeded is thrown.
public interface IAiInvocationRecorder
{
    Task<AiInvocationResult<T>> RecordAsync<T>(
        AiInvocationContext context,
        Func<CancellationToken, Task<AiInvocationCall<T>>> call,
        CancellationToken cancellationToken = default);
}

public sealed record AiInvocationContext(
    AiInvocationTrigger Trigger,
    string InputText,
    Guid? TriggeredByUserId,
    Guid? ProjectId,
    Guid? TicketId,
    string? Reason = null);

// The lambda must surface both the payload and token accounting so we can record it.
public sealed record AiInvocationCall<T>(T Payload, int PromptTokens, int CompletionTokens, string? OutputPreview);

public sealed record AiInvocationResult<T>(T Payload, Guid InvocationId);

public sealed class AiRateLimitExceededException(string message) : Exception(message);

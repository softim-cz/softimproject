namespace SoftimProject.Domain.Enums;

// Namespace for operations that can end up in the dead-letter queue. Extend with
// care: the Replayer dispatches on this enum, so a new value without a registered
// handler means admins can list/dismiss rows but not replay them.
public enum DeadLetterOperation
{
    AiSummarizeTicket,
    GitHubSyncProject,
    EasyProjectFetch,
    GitHubWebhook
}

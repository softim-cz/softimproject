namespace SoftimProject.Application.Interfaces;

// Constants for named Polly pipelines registered at Infrastructure DI. Using strings
// via ResiliencePipelineProvider<string> keeps call sites decoupled from the concrete
// pipeline configuration.
public static class ResiliencePipelines
{
    public const string AiApi = "ai-api";
    public const string GitHubApi = "github-api";
}

namespace SoftimProject.Infrastructure.Services;

// Lightweight cost estimator. Rates are snapshot values — override from config
// under `Ai:Pricing:*` when your Azure commitment differs. Zero is a valid
// value (dev / free-tier deployments).
internal sealed class AiPricing
{
    public string Model { get; init; } = "gpt-4o";
    public decimal InputPerMillionTokensUsd { get; init; } = 2.50m;
    public decimal OutputPerMillionTokensUsd { get; init; } = 10.00m;

    public decimal EstimateCostUsd(int promptTokens, int completionTokens)
    {
        var input = (decimal)promptTokens / 1_000_000m * InputPerMillionTokensUsd;
        var output = (decimal)completionTokens / 1_000_000m * OutputPerMillionTokensUsd;
        return decimal.Round(input + output, 6);
    }
}

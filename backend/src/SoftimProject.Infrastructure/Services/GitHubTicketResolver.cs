using System.Text.RegularExpressions;

namespace SoftimProject.Infrastructure.Services;

// Extracts a (project code, ticket number) hint from GitHub inputs — branch name,
// PR title, PR body. The format we emit from the Create Branch flow is
// `feat/<PROJECT>-<NUMBER>-<slug>` but users frequently rename branches by hand,
// so we accept any occurrence of `<PROJECT>-<NUMBER>` where PROJECT is uppercase
// letters and NUMBER is an integer. The first match wins.
public static class GitHubTicketResolver
{
    // Uppercase letters, 2–10 chars, hyphen, 1–7 digits — mirrors the Code validator
    // in CreateProjectCommand (code is `^[A-Z]+$`, length 2–6 in practice).
    private static readonly Regex KeyPattern = new(
        @"(?<code>[A-Z][A-Z0-9]{1,9})-(?<number>\d{1,7})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static TicketKey? TryResolve(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            var match = KeyPattern.Match(candidate.ToUpperInvariant());
            if (match.Success && int.TryParse(match.Groups["number"].Value, out var number))
            {
                return new TicketKey(match.Groups["code"].Value, number);
            }
        }
        return null;
    }
}

public sealed record TicketKey(string ProjectCode, int TicketNumber);

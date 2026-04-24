using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Octokit;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record CreateTicketBranchCommand(Guid ProjectId, Guid TicketId)
    : IRequest<CreateTicketBranchResult>, IRequireProjectRole
{
    // Developer+ can create branches — keeps parity with ticket update rights. Guest
    // explicitly cannot (there's no reason a client-portal user should push refs).
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed record CreateTicketBranchResult(string BranchName, string BranchUrl);

public sealed class CreateTicketBranchCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<CreateTicketBranchCommand, CreateTicketBranchResult>
{
    public async Task<CreateTicketBranchResult> Handle(CreateTicketBranchCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException();

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new SoftimProject.Application.Common.NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        if (project.ExternalSystem != "GitHub" || string.IsNullOrWhiteSpace(project.ExternalProjectId))
            throw new InvalidOperationException("Project is not linked to a GitHub repository.");

        var ticket = await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new SoftimProject.Application.Common.NotFoundException(nameof(Domain.Entities.Ticket), request.TicketId);

        // Prefer the caller's OAuth token (they are pushing the ref in their name);
        // fall back to the project-level token recorded when the repo was linked.
        var token = await dbContext.Users
            .Where(u => u.Id == currentUser.UserId.Value)
            .Select(u => u.GitHubAccessToken)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token) && project.GitHubConnectedByUserId.HasValue)
        {
            token = await dbContext.Users
                .Where(u => u.Id == project.GitHubConnectedByUserId.Value)
                .Select(u => u.GitHubAccessToken)
                .FirstOrDefaultAsync(cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("No GitHub access token available. Connect your GitHub account first.");

        var parts = project.ExternalProjectId.Split('/');
        if (parts.Length != 2)
            throw new InvalidOperationException("Invalid repository format on the project.");
        var owner = parts[0];
        var repo = parts[1];

        var branchName = BuildBranchName(project.Code, ticket.Number, ticket.Title);

        var client = new GitHubClient(new ProductHeaderValue("SoftimProject"))
        {
            Credentials = new Credentials(token),
        };

        // Resolve the repo default branch and the sha it points at — the new ref is
        // created from there. Doing this through the API (instead of hard-coding "main")
        // handles repos that still use "master" or a custom default.
        var repoInfo = await client.Repository.Get(owner, repo);
        var defaultBranch = repoInfo.DefaultBranch;
        var baseRef = await client.Git.Reference.Get(owner, repo, $"heads/{defaultBranch}");
        var baseSha = baseRef.Object.Sha;

        // If the branch already exists, treat as success (idempotent "Create branch" click).
        try
        {
            var existing = await client.Git.Reference.Get(owner, repo, $"heads/{branchName}");
            return new CreateTicketBranchResult(branchName, repoInfo.HtmlUrl + "/tree/" + Uri.EscapeDataString(branchName));
        }
        catch (Octokit.NotFoundException)
        {
            // expected — fall through to create
        }

        await client.Git.Reference.Create(owner, repo, new NewReference($"refs/heads/{branchName}", baseSha));
        return new CreateTicketBranchResult(branchName, repoInfo.HtmlUrl + "/tree/" + Uri.EscapeDataString(branchName));
    }

    // `feat/<PROJECT>-<NUMBER>-<slug>` — slug from title, lowercased ASCII, hyphen-separated,
    // trimmed to 40 chars to stay well under Git's 250-char branch limit.
    private static string BuildBranchName(string projectCode, int number, string title)
    {
        var slug = Slugify(title);
        var code = projectCode.ToUpperInvariant();
        return string.IsNullOrEmpty(slug)
            ? $"feat/{code}-{number}"
            : $"feat/{code}-{number}-{slug}";
    }

    private static readonly Regex NonAlnum = new("[^a-z0-9]+", RegexOptions.Compiled);

    // Regex collapses any non-a-z0-9 run (including non-ASCII — "čau" → "-au") into a
    // single hyphen. Good enough for branch names; the branch is a hint, the PR still
    // links back via the ticket key embedded in the name.
    private static string Slugify(string input)
    {
        var slug = NonAlnum.Replace(input.ToLowerInvariant(), "-").Trim('-');
        return slug.Length > 40 ? slug[..40].Trim('-') : slug;
    }
}

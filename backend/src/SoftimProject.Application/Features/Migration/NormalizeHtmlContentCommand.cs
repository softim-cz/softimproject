using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration;

public sealed record NormalizeHtmlContentResult(int Tickets, int Comments, int Projects);

/// <summary>
/// One-off maintenance: convert HTML left over from imports (ticket/project
/// descriptions, comment bodies) into Markdown so the renderer shows it readably.
/// New imports already convert on the fly; this fixes pre-existing rows.
/// </summary>
public sealed record NormalizeHtmlContentCommand : IRequest<NormalizeHtmlContentResult>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class NormalizeHtmlContentCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<NormalizeHtmlContentCommand, NormalizeHtmlContentResult>
{
    public async Task<NormalizeHtmlContentResult> Handle(NormalizeHtmlContentCommand request, CancellationToken cancellationToken)
    {
        var tickets = await dbContext.Tickets
            .Where(t => t.Description != null && t.Description.Contains("<"))
            .ToListAsync(cancellationToken);
        var ticketCount = 0;
        foreach (var t in tickets)
        {
            if (!HtmlToMarkdown.LooksLikeHtml(t.Description)) continue;
            var converted = HtmlToMarkdown.Convert(t.Description);
            if (converted != t.Description) { t.Description = converted; ticketCount++; }
        }

        var comments = await dbContext.Comments
            .Where(c => c.Content.Contains("<"))
            .ToListAsync(cancellationToken);
        var commentCount = 0;
        foreach (var c in comments)
        {
            if (!HtmlToMarkdown.LooksLikeHtml(c.Content)) continue;
            var converted = HtmlToMarkdown.Convert(c.Content) ?? c.Content;
            if (converted != c.Content) { c.Content = converted; commentCount++; }
        }

        var projects = await dbContext.Projects
            .Where(p => p.Description != null && p.Description.Contains("<"))
            .ToListAsync(cancellationToken);
        var projectCount = 0;
        foreach (var p in projects)
        {
            if (!HtmlToMarkdown.LooksLikeHtml(p.Description)) continue;
            var converted = HtmlToMarkdown.Convert(p.Description);
            if (converted != p.Description) { p.Description = converted; projectCount++; }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new NormalizeHtmlContentResult(ticketCount, commentCount, projectCount);
    }
}

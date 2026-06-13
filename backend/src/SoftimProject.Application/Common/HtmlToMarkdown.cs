using System.Net;
using System.Text.RegularExpressions;

namespace SoftimProject.Application.Common;

/// <summary>
/// Minimal, dependency-free HTML → Markdown converter for content imported from
/// external systems (EasyProject/Redmine emit HTML descriptions/comments). The
/// app renders descriptions with a strict, raw-HTML-free Markdown renderer, so
/// stored HTML shows up as unreadable markup. Converting to Markdown on import
/// makes it both render and edit cleanly. Not a full HTML parser — it handles the
/// common tags EP produces and strips the rest.
/// </summary>
public static class HtmlToMarkdown
{
    private static readonly Regex AnyTag = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex LooksHtml = new("<(/?)([a-zA-Z][a-zA-Z0-9]*)(\\s[^>]*)?>", RegexOptions.Compiled);
    private static readonly Regex Anchor = new(
        "<a\\s[^>]*?href\\s*=\\s*[\"'](?<href>[^\"']*)[\"'][^>]*>(?<text>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>True when the string contains HTML tags worth converting.</summary>
    public static bool LooksLikeHtml(string? input) =>
        !string.IsNullOrEmpty(input) && LooksHtml.IsMatch(input);

    /// <summary>
    /// Converts HTML to Markdown. Returns the input unchanged when it is null/blank
    /// or doesn't look like HTML (so already-Markdown content is left intact).
    /// </summary>
    public static string? Convert(string? html)
    {
        if (string.IsNullOrWhiteSpace(html) || !LooksLikeHtml(html))
            return html;

        var s = html.Replace("\r\n", "\n").Replace("\r", "\n");

        // Links first (before stripping tags inside the label).
        s = Anchor.Replace(s, m =>
        {
            var href = m.Groups["href"].Value.Trim();
            var text = Strip(m.Groups["text"].Value).Trim();
            if (string.IsNullOrEmpty(text)) text = href;
            return string.IsNullOrEmpty(href) ? text : $"[{text}]({href})";
        });

        // Headings.
        for (var i = 1; i <= 6; i++)
        {
            var prefix = new string('#', i);
            s = Regex.Replace(s, $"<h{i}[^>]*>(.*?)</h{i}>",
                m => $"\n\n{prefix} {Strip(m.Groups[1].Value).Trim()}\n\n",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        // Pre / code blocks (before inline code).
        s = Regex.Replace(s, "<pre[^>]*>(.*?)</pre>",
            m => $"\n\n```\n{Strip(m.Groups[1].Value).Trim()}\n```\n\n",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        s = Regex.Replace(s, "<code[^>]*>(.*?)</code>",
            m => $"`{Strip(m.Groups[1].Value)}`",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Blockquote.
        s = Regex.Replace(s, "<blockquote[^>]*>(.*?)</blockquote>",
            m => "\n\n> " + Strip(m.Groups[1].Value).Trim().Replace("\n", "\n> ") + "\n\n",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Emphasis.
        s = Regex.Replace(s, "</?(strong|b)\\s*>", "**", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "</?(em|i)\\s*>", "*", RegexOptions.IgnoreCase);

        // List items.
        s = Regex.Replace(s, "<li[^>]*>(.*?)</li>",
            m => $"- {Strip(m.Groups[1].Value).Trim()}\n",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        s = Regex.Replace(s, "</?(ul|ol)[^>]*>", "\n", RegexOptions.IgnoreCase);

        // Line breaks & block boundaries.
        s = Regex.Replace(s, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "</p\\s*>", "\n\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "</div\\s*>", "\n", RegexOptions.IgnoreCase);

        // Drop any remaining tags, then decode entities.
        s = Strip(s);
        s = WebUtility.HtmlDecode(s);

        // Tidy whitespace.
        s = s.Replace((char)0xA0, ' ');
        s = Regex.Replace(s, "[ \\t]+\n", "\n");
        s = Regex.Replace(s, "\n{3,}", "\n\n");

        return s.Trim();
    }

    private static string Strip(string input) => AnyTag.Replace(input, string.Empty);
}

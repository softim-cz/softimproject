using SoftimProject.Application.Common;
using Xunit;

namespace SoftimProject.Application.Tests;

public class HtmlToMarkdownTests
{
    [Fact]
    public void Leaves_Plain_Text_Untouched()
    {
        const string input = "Just plain text, no markup.";
        Assert.Equal(input, HtmlToMarkdown.Convert(input));
    }

    [Fact]
    public void Leaves_Existing_Markdown_Untouched()
    {
        const string input = "# Heading\n\n- item one\n- item two";
        Assert.Equal(input, HtmlToMarkdown.Convert(input));
    }

    [Fact]
    public void Converts_Paragraphs_And_Emphasis()
    {
        var result = HtmlToMarkdown.Convert("<p>Hello <strong>bold</strong> and <em>italic</em>.</p><p>Second.</p>");
        Assert.Equal("Hello **bold** and *italic*.\n\nSecond.", result);
    }

    [Fact]
    public void Converts_Links()
    {
        var result = HtmlToMarkdown.Convert("<p>See <a href=\"https://x.test/a\">the docs</a> now.</p>");
        Assert.Equal("See [the docs](https://x.test/a) now.", result);
    }

    [Fact]
    public void Converts_Unordered_List()
    {
        var result = HtmlToMarkdown.Convert("<ul><li>one</li><li>two</li></ul>");
        Assert.Contains("- one", result);
        Assert.Contains("- two", result);
    }

    [Fact]
    public void Decodes_Entities_And_Strips_Unknown_Tags()
    {
        var result = HtmlToMarkdown.Convert("<span class=\"x\">A &amp; B &lt; C</span>");
        Assert.Equal("A & B < C", result);
    }

    [Fact]
    public void LooksLikeHtml_Detects_Tags()
    {
        Assert.True(HtmlToMarkdown.LooksLikeHtml("<p>hi</p>"));
        Assert.False(HtmlToMarkdown.LooksLikeHtml("plain 1 < 2 text"));
        Assert.False(HtmlToMarkdown.LooksLikeHtml(null));
    }
}

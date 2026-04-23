using FluentAssertions;
using SoftimProject.WebApi.Controllers;

namespace SoftimProject.WebApi.Tests.Portal;

// Shape tests — fail fast if someone adds an internal property to a Portal DTO
// that leaks data to anonymous clients. Keeps the masking contract explicit.
public class PortalDtoShapeTests
{
    [Fact]
    public void PortalTicketDto_DoesNotExposeInternalFields()
    {
        var forbidden = new[]
        {
            "Description",
            "EstimatedHours",
            "CumulativeWorkedHours",
            "Comments",
            "IsBillable",
            "Invoiced",
            "CreatedBy",
            "CreatedAt",
        };

        var properties = typeof(PortalTicketDto).GetProperties().Select(p => p.Name).ToArray();

        properties.Should().NotIntersectWith(forbidden,
            "PortalTicketDto is returned to anonymous portal clients and must not leak internal fields.");
    }

    [Fact]
    public void PortalProjectDto_DoesNotExposeInternalFields()
    {
        var forbidden = new[]
        {
            "ClientAccessToken",
            "ClientAccessEnabled",
            "ProjectTemplateId",
            "GitHubOwner",
            "GitHubRepo",
            "GitHubInstallationId",
        };

        var properties = typeof(PortalProjectDto).GetProperties().Select(p => p.Name).ToArray();

        properties.Should().NotIntersectWith(forbidden,
            "PortalProjectDto is returned to anonymous portal clients and must not leak credentials/internal routing.");
    }

    [Fact]
    public void PortalResponseDto_CommentsPropertyExists_AndIsMaskedList()
    {
        // PortalController currently returns Array.Empty<object>() for Comments.
        // If this contract changes to return real comments, the type below will change,
        // and the masking policy needs explicit review (internal vs non-internal filter).
        var commentsProperty = typeof(PortalResponseDto).GetProperty("Comments");
        commentsProperty.Should().NotBeNull("PortalResponseDto must have a Comments property");
        commentsProperty!.PropertyType.Name.Should().Contain("IReadOnlyList",
            "Comments is a masked list contract; changing its shape requires re-reviewing the masking policy.");
    }

    [Fact]
    public void PortalUserDto_OnlyExposesDisplayName()
    {
        // Assignee on a portal ticket must not leak email/entra IDs/login names.
        var properties = typeof(PortalUserDto).GetProperties().Select(p => p.Name).ToArray();

        properties.Should().BeEquivalentTo(new[] { "Id", "DisplayName" },
            "PortalUserDto must only expose display name and opaque id.");
    }
}

using FluentAssertions;
using SoftimProject.Infrastructure.Services.EasyProject;

namespace SoftimProject.Infrastructure.Tests;

public class EasyProjectApiClientFilterTests
{
    [Fact]
    public void BuildUpdatedSinceFilter_Null_ReturnsNull()
        => EasyProjectApiClient.BuildUpdatedSinceFilter(null).Should().BeNull();

    [Fact]
    public void BuildUpdatedSinceFilter_EncodesOperatorAndTimestamp_AsUtc()
    {
        // Non-UTC input must be normalized to UTC; ">=" and ":" URL-encoded.
        var since = new DateTime(2026, 6, 27, 10, 30, 0, DateTimeKind.Utc);

        var filter = EasyProjectApiClient.BuildUpdatedSinceFilter(since);

        filter.Should().Be("updated_on=%3E%3D2026-06-27T10%3A30%3A00Z");
    }
}

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.Infrastructure.Tests;

public class CurrentUserProvisioningTests
{
    [Fact]
    public async Task Provisioning_fills_corporate_role_and_company_from_directory()
    {
        await using var db = CreateDbContext();
        var oid = Guid.NewGuid().ToString();
        var svc = new CurrentUserService(
            MockAccessor(Principal(oid)),
            db,
            new StubDirectory(new DirectoryUserProfile("Vývojář", "Softim s.r.o.")));

        await svc.InitializeAsync();

        var user = await db.Users.FirstAsync(u => u.EntraObjectId == oid);
        user.CorporateRole.Should().Be("Vývojář");
        user.CompanyName.Should().Be("Softim s.r.o.");
        user.DisplayName.Should().Be("Jan Novák");
        user.FirstName.Should().Be("Jan");
    }

    [Fact]
    public async Task Provisioning_without_directory_profile_leaves_fields_null()
    {
        await using var db = CreateDbContext();
        var oid = Guid.NewGuid().ToString();
        var svc = new CurrentUserService(MockAccessor(Principal(oid)), db, new StubDirectory(null));

        await svc.InitializeAsync();

        svc.UserId.Should().NotBeNull();
        var user = await db.Users.FirstAsync(u => u.EntraObjectId == oid);
        user.CorporateRole.Should().BeNull();
        user.CompanyName.Should().BeNull();
    }

    private static ClaimsPrincipal Principal(string oid) =>
        new(new ClaimsIdentity(
            new[]
            {
                new Claim("oid", oid),
                new Claim("preferred_username", "jan@softim.cz"),
                new Claim("name", "Jan Novák"),
                new Claim("given_name", "Jan"),
                new Claim("family_name", "Novák"),
            },
            "test"));

    private static IHttpContextAccessor MockAccessor(ClaimsPrincipal principal)
    {
        var ctx = new Mock<HttpContext>();
        ctx.SetupGet(c => c.User).Returns(principal);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(ctx.Object);
        return accessor.Object;
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class StubDirectory(DirectoryUserProfile? profile) : IUserDirectory
    {
        public Task<DirectoryUserProfile?> GetProfileAsync(
            string entraObjectId, CancellationToken cancellationToken = default)
            => Task.FromResult(profile);
    }
}

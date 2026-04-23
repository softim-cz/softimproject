using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// Drives the full ASP.NET Core pipeline against an in-memory EF Core provider so authorization
// boundaries can be asserted end-to-end. DevAuth is left registered (Env=Development) but
// DatabaseMigration:AutoApply is turned off — tests create the schema via EnsureCreated.
// SQLite was tried first but fails because EF Core model configurations use SQL Server-specific
// column types (nvarchar(max)); the in-memory provider ignores column types and is sufficient
// for authorization pipeline checks. All IHostedService registrations are removed so background
// workers don't poll the test DB.
public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = $"AuthBoundaryTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevAuth:Enabled"] = "true",
                ["DevAuth:SeedOnStartup"] = "false",
                ["DatabaseMigration:AutoApply"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=test.db",
                ["Frontend:BaseUrl"] = "http://localhost:3000",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Strip every EF Core registration attached to ApplicationDbContext — options, internal
            // services, factories — before re-adding the in-memory variant. If we leave any remnants
            // of the SQL Server provider in place, EF's provider cache errors with
            // "Services for database providers ... have been registered".
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(ApplicationDbContext)
                    || (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false)
                    || (d.ImplementationType?.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false))
                .ToList();
            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_dbName));
            services.RemoveAll<IHostedService>();
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedAsync(db);
    }

    public new Task DisposeAsync()
    {
        base.DisposeAsync().GetAwaiter().GetResult();
        return Task.CompletedTask;
    }
}

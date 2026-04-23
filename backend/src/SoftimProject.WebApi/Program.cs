using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using SoftimProject.Infrastructure;
using SoftimProject.Infrastructure.Options;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Persistence.Seeding;
using SoftimProject.Application;
using SoftimProject.WebApi.Authentication;
using SoftimProject.WebApi.Hubs;
using SoftimProject.WebApi.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SoftimProject Web API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Add layers
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // GitHub OAuth
    builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection(GitHubOptions.SectionName));
    builder.Services.AddHttpClient();

    // Authentication & Authorization
    var devAuthEnabled = builder.Environment.IsDevelopment()
        && builder.Configuration.GetValue<bool>("DevAuth:Enabled");

    if (devAuthEnabled)
    {
        Log.Warning("DevAuth scheme is ENABLED. This must never run in Production.");
        builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
                DevAuthenticationHandler.SchemeName, _ => { });
    }
    else
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    }
    builder.Services.AddAuthorization();

    // Allow SignalR to read the access token from the query string (WebSockets/SSE don't support headers)
    if (!devAuthEnabled)
    {
        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var existingOnMessageReceived = options.Events?.OnMessageReceived;
            options.Events ??= new JwtBearerEvents();
            options.Events.OnMessageReceived = async context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                if (existingOnMessageReceived != null)
                {
                    await existingOnMessageReceived(context);
                }
            };
        });
    }

    // API
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()));
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // SignalR
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<SoftimProject.Application.Interfaces.IMigrationNotifier, SoftimProject.WebApi.Services.MigrationNotifier>();

    // CORS
    var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(frontendUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Seeder (dev only).
    if (devAuthEnabled)
    {
        builder.Services.AddScoped<DatabaseSeeder>();
    }

    // Health Checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Apply pending EF Core migrations on startup. Fail fast if the DB is unreachable.
    // Integration tests provision schema via EnsureCreated on SQLite, so they set this flag to false.
    if (app.Configuration.GetValue("DatabaseMigration:AutoApply", true))
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Log.Information("Applying pending database migrations...");
            await db.Database.MigrateAsync();
            Log.Information("Database migrations up to date");

            if (devAuthEnabled && app.Configuration.GetValue<bool>("DevAuth:SeedOnStartup"))
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
                Log.Information("Database seed complete");
            }
        }
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<CurrentUserMiddleware>();

    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapHub<KanbanHub>("/hubs/kanban");
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHub<MigrationHub>("/hubs/migration");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program { }

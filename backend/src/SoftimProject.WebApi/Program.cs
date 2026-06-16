using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
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

    string primaryScheme;
    var authBuilder = builder.Services.AddAuthentication(
        devAuthEnabled ? DevAuthenticationHandler.SchemeName : JwtBearerDefaults.AuthenticationScheme);

    if (devAuthEnabled)
    {
        Log.Warning("DevAuth scheme is ENABLED. This must never run in Production.");
        primaryScheme = DevAuthenticationHandler.SchemeName;
        authBuilder.AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
            DevAuthenticationHandler.SchemeName, _ => { });
    }
    else
    {
        primaryScheme = JwtBearerDefaults.AuthenticationScheme;
        authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    }

    // Personal API keys work as a second scheme in both environments.
    authBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });

    // Default [Authorize] policy accepts EITHER the interactive scheme (Entra JWT / Dev)
    // OR a personal API key — headless API clients send `Authorization: Bearer spk_…`.
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(primaryScheme, ApiKeyAuthenticationHandler.SchemeName)
            .Build();
    });

    // Rate limiting — only throttles API-key (headless/script) traffic so it can't
    // hammer the API; interactive (Entra/Dev) requests are not limited. 120 req/min
    // per API-key user, then 429 + Retry-After.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var isApiKey = httpContext.User?.FindFirst("auth_method")?.Value == "api_key";
            if (!isApiKey)
                return RateLimitPartition.GetNoLimiter("interactive");

            var partitionKey = httpContext.User?.FindFirst("oid")?.Value ?? "api-key";
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
        });
        options.OnRejected = async (context, token) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString();
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { message = "Rate limit exceeded. Please slow down." }, token);
        };
    });

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
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SoftimProject API",
            Version = "v1",
            Description = "API pro práci s projekty, tickety a worklogy. "
                + "Autentizace: Entra JWT nebo osobní API klíč (spk_…) v hlavičce Authorization: Bearer.",
        });

        // Pick up XML doc comments when present (see #103).
        foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
        {
            try { options.IncludeXmlComments(xml, includeControllerXmlComments: true); }
            catch { /* not all assemblies emit XML */ }
        }

        // Entra JWT or personal API key — both ride on Authorization: Bearer.
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT or spk_…",
            In = ParameterLocation.Header,
            Description = "Vlož Entra JWT NEBO osobní API klíč (spk_…). 'Bearer ' se doplní automaticky.",
        });
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Name = "X-Api-Key",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Osobní API klíč (spk_…) v hlavičce X-Api-Key.",
        });
        options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer", doc, null), new List<string>() },
            { new OpenApiSecuritySchemeReference("ApiKey", doc, null), new List<string>() },
        });
    });

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

    // CORS. Frontend:BaseUrl may list several origins separated by ',' or ';' so the web
    // app can be reached under more than one host at once (e.g. a custom domain plus the
    // azurewebsites.net fallback during a domain cutover). The first entry is also used as
    // the canonical base for notification/email links (see Frontend:BaseUrl consumers).
    var frontendOrigins = (builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:3000")
        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(frontendOrigins)
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

    // Swagger UI: always in Development, and in other environments when explicitly
    // enabled (Swagger:Enabled). API endpoints stay [Authorize], so the UI only
    // exposes the API shape — callers still need a token/API key to invoke anything.
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SoftimProject API v1");
            options.DocumentTitle = "SoftimProject API";
        });
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.UseMiddleware<CurrentUserMiddleware>();

    app.MapControllers();
    app.MapHealthChecks("/health");

    // Unversioned alias for the versioned HealthController.GetJobs at /api/v1/health/jobs.
    // Probes and the smoke-check curl in CONTRIBUTING expect /health/jobs (consistent with
    // the unversioned /health liveness probe), so we expose both. Same anonymous access,
    // same payload, same 503-on-degraded contract.
    app.MapGet("/health/jobs",
        async (MediatR.ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new SoftimProject.Application.Features.Health.GetJobsHealthQuery(), ct);
            return result.Status == "Healthy"
                ? Results.Ok(result)
                : Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }).AllowAnonymous();

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

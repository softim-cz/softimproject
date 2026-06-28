using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Admin;

public sealed record IntegrationConnectionDto(
    Guid Id,
    string Name,
    SyncType SourceSystem,
    string BaseUrl,
    IntegrationSyncMode Mode,
    bool IsEnabled,
    int IntervalMinutes,
    ConflictPolicy ConflictPolicy,
    Guid? TargetCompanyId,
    string? TargetCompanyName,
    bool HasToken,
    DateTime? LastSyncStartedAt,
    DateTime? LastSyncWatermark,
    int ProjectsCount);

// --- List ---

public sealed record GetIntegrationConnectionsQuery : IRequest<List<IntegrationConnectionDto>>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class GetIntegrationConnectionsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetIntegrationConnectionsQuery, List<IntegrationConnectionDto>>
{
    public async Task<List<IntegrationConnectionDto>> Handle(GetIntegrationConnectionsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.IntegrationConnections
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new IntegrationConnectionDto(
                c.Id,
                c.Name,
                c.SourceSystem,
                c.BaseUrl,
                c.Mode,
                c.IsEnabled,
                c.IntervalMinutes,
                c.ConflictPolicy,
                c.TargetCompanyId,
                c.TargetCompany != null ? c.TargetCompany.Name : null,
                c.EncryptedApiToken != null,
                c.LastSyncStartedAt,
                c.LastSyncWatermark,
                dbContext.Projects.Count(p => p.IntegrationConnectionId == c.Id)))
            .ToListAsync(cancellationToken);
    }
}

// --- Update settings (scheduling/policy/company; credentials & mappings stay with the wizard) ---

public sealed record UpdateIntegrationConnectionCommand(
    Guid Id,
    IntegrationSyncMode Mode,
    bool IsEnabled,
    int IntervalMinutes,
    ConflictPolicy ConflictPolicy,
    Guid? TargetCompanyId) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateIntegrationConnectionCommandValidator : AbstractValidator<UpdateIntegrationConnectionCommand>
{
    public UpdateIntegrationConnectionCommandValidator()
    {
        RuleFor(x => x.IntervalMinutes)
            .GreaterThanOrEqualTo(60)
            .When(x => x.IsEnabled)
            .WithMessage("Interval synchronizace musí být alespoň 60 minut.");
    }
}

public sealed class UpdateIntegrationConnectionCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateIntegrationConnectionCommand>
{
    public async Task Handle(UpdateIntegrationConnectionCommand request, CancellationToken cancellationToken)
    {
        var connection = await dbContext.IntegrationConnections
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationConnection), request.Id);

        connection.Mode = request.Mode;
        connection.IsEnabled = request.IsEnabled;
        connection.IntervalMinutes = request.IntervalMinutes;
        connection.ConflictPolicy = request.ConflictPolicy;
        connection.TargetCompanyId = request.TargetCompanyId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// --- Delete ---

public sealed record DeleteIntegrationConnectionCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DeleteIntegrationConnectionCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteIntegrationConnectionCommand>
{
    public async Task Handle(DeleteIntegrationConnectionCommand request, CancellationToken cancellationToken)
    {
        var connection = await dbContext.IntegrationConnections
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationConnection), request.Id);

        // Linked projects keep their data; the FK is set null (configured ON DELETE SET NULL).
        dbContext.IntegrationConnections.Remove(connection);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// --- Trigger an immediate sync (fire-and-forget on its own scope) ---

public sealed record TriggerIntegrationSyncCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class TriggerIntegrationSyncCommandHandler(
    IApplicationDbContext dbContext,
    IServiceScopeFactory scopeFactory) : IRequestHandler<TriggerIntegrationSyncCommand>
{
    public async Task Handle(TriggerIntegrationSyncCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.IntegrationConnections.AnyAsync(c => c.Id == request.Id, cancellationToken);
        if (!exists) throw new NotFoundException(nameof(IntegrationConnection), request.Id);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var trigger = scope.ServiceProvider.GetRequiredService<IIntegrationSyncTrigger>();
                await trigger.RunNowAsync(request.Id, CancellationToken.None);
            }
            catch
            {
                // Best-effort; the runner records its own failures (SyncLog / DLQ).
            }
        });
    }
}

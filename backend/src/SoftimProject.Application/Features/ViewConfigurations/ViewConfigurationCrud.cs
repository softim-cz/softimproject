using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.ViewConfigurations;

// DTO
public sealed record ViewConfigurationDto(Guid Id, Guid UserId, Guid? ProjectId, string ViewType, string ConfigurationJson);

// GET
public sealed record GetViewConfigurationQuery(Guid UserId, Guid? ProjectId, string ViewType) : IRequest<ViewConfigurationDto?>;

public sealed class GetViewConfigurationQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetViewConfigurationQuery, ViewConfigurationDto?>
{
    public async Task<ViewConfigurationDto?> Handle(GetViewConfigurationQuery request, CancellationToken cancellationToken)
    {
        var config = await dbContext.ViewConfigurations
            .Where(vc => vc.UserId == request.UserId
                         && vc.ProjectId == request.ProjectId
                         && vc.ViewType == request.ViewType)
            .Select(vc => new ViewConfigurationDto(vc.Id, vc.UserId, vc.ProjectId, vc.ViewType, vc.ConfigurationJson))
            .FirstOrDefaultAsync(cancellationToken);

        return config;
    }
}

// UPSERT
public sealed record UpsertViewConfigurationCommand(Guid? ProjectId, string ViewType, string ConfigurationJson) : IRequest<Guid>;

public sealed class UpsertViewConfigurationCommandValidator : AbstractValidator<UpsertViewConfigurationCommand>
{
    public UpsertViewConfigurationCommandValidator()
    {
        RuleFor(x => x.ViewType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ConfigurationJson).NotEmpty();
    }
}

public sealed class UpsertViewConfigurationCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<UpsertViewConfigurationCommand, Guid>
{
    public async Task<Guid> Handle(UpsertViewConfigurationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var existing = await dbContext.ViewConfigurations
            .FirstOrDefaultAsync(vc => vc.UserId == userId
                                       && vc.ProjectId == request.ProjectId
                                       && vc.ViewType == request.ViewType, cancellationToken);

        if (existing is not null)
        {
            existing.ConfigurationJson = request.ConfigurationJson;
            await dbContext.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var config = new ViewConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = request.ProjectId,
            ViewType = request.ViewType,
            ConfigurationJson = request.ConfigurationJson,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ViewConfigurations.Add(config);
        await dbContext.SaveChangesAsync(cancellationToken);
        return config.Id;
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.SavedFilters;

// DTO
public sealed record SavedFilterDto(
    Guid Id, string Name, Guid? UserId, Guid? ProjectId,
    string ViewType, string FilterJson, bool IsSystem, int SortOrder);

// GET ALL
public sealed record GetSavedFiltersQuery(Guid? ProjectId, string ViewType) : IRequest<List<SavedFilterDto>>;

public sealed class GetSavedFiltersQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetSavedFiltersQuery, List<SavedFilterDto>>
{
    public async Task<List<SavedFilterDto>> Handle(GetSavedFiltersQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var query = dbContext.SavedFilters
            .Where(sf => sf.ViewType == request.ViewType)
            .Where(sf => sf.IsSystem || sf.UserId == userId);

        if (request.ProjectId.HasValue)
            query = query.Where(sf => sf.ProjectId == request.ProjectId.Value || sf.ProjectId == null);

        return await query
            .OrderBy(sf => sf.SortOrder)
            .ThenBy(sf => sf.Name)
            .Select(sf => new SavedFilterDto(
                sf.Id, sf.Name, sf.UserId, sf.ProjectId,
                sf.ViewType, sf.FilterJson, sf.IsSystem, sf.SortOrder))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateSavedFilterCommand(
    string Name, Guid? ProjectId, string ViewType,
    string FilterJson, bool IsSystem, int SortOrder) : IRequest<Guid>;

public sealed class CreateSavedFilterCommandValidator : AbstractValidator<CreateSavedFilterCommand>
{
    public CreateSavedFilterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ViewType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FilterJson).NotEmpty();
    }
}

public sealed class CreateSavedFilterCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<CreateSavedFilterCommand, Guid>
{
    public async Task<Guid> Handle(CreateSavedFilterCommand request, CancellationToken cancellationToken)
    {
        var filter = new SavedFilter
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            UserId = request.IsSystem ? null : currentUserService.UserId,
            ProjectId = request.ProjectId,
            ViewType = request.ViewType,
            FilterJson = request.FilterJson,
            IsSystem = request.IsSystem,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.SavedFilters.Add(filter);
        await dbContext.SaveChangesAsync(cancellationToken);
        return filter.Id;
    }
}

// UPDATE
public sealed record UpdateSavedFilterCommand(Guid Id, string Name, string FilterJson, int SortOrder) : IRequest;

public sealed class UpdateSavedFilterCommandValidator : AbstractValidator<UpdateSavedFilterCommand>
{
    public UpdateSavedFilterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FilterJson).NotEmpty();
    }
}

public sealed class UpdateSavedFilterCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateSavedFilterCommand>
{
    public async Task Handle(UpdateSavedFilterCommand request, CancellationToken cancellationToken)
    {
        var filter = await dbContext.SavedFilters
            .FirstOrDefaultAsync(sf => sf.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SavedFilter), request.Id);

        filter.Name = request.Name;
        filter.FilterJson = request.FilterJson;
        filter.SortOrder = request.SortOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteSavedFilterCommand(Guid Id) : IRequest;

public sealed class DeleteSavedFilterCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteSavedFilterCommand>
{
    public async Task Handle(DeleteSavedFilterCommand request, CancellationToken cancellationToken)
    {
        var filter = await dbContext.SavedFilters
            .FirstOrDefaultAsync(sf => sf.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SavedFilter), request.Id);

        dbContext.SavedFilters.Remove(filter);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

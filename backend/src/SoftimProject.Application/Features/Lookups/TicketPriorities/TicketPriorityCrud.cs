using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.TicketPriorities;

// DTO
public sealed record TicketPriorityDto(Guid Id, string Name, string? NameCs, string? NameEn, string Color, int SortOrder, bool IsActive, bool IsDefault, Guid ProjectTemplateId);

// GET ALL
public sealed record GetTicketPrioritiesQuery(Guid? ProjectTemplateId = null) : IRequest<List<TicketPriorityDto>>;

public sealed class GetTicketPrioritiesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTicketPrioritiesQuery, List<TicketPriorityDto>>
{
    public async Task<List<TicketPriorityDto>> Handle(GetTicketPrioritiesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TicketPriorities.AsQueryable();

        if (request.ProjectTemplateId.HasValue)
            query = query.Where(tp => tp.ProjectTemplateId == request.ProjectTemplateId.Value);

        return await query
            .OrderBy(tp => tp.SortOrder).ThenBy(tp => tp.Name)
            .Select(tp => new TicketPriorityDto(tp.Id, tp.Name, tp.NameCs, tp.NameEn, tp.Color, tp.SortOrder, tp.IsActive, tp.IsDefault, tp.ProjectTemplateId))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateTicketPriorityCommand(string Name, string? NameCs, string? NameEn, string Color, int SortOrder, bool IsDefault, Guid ProjectTemplateId) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class CreateTicketPriorityCommandValidator : AbstractValidator<CreateTicketPriorityCommand>
{
    public CreateTicketPriorityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProjectTemplateId).NotEmpty();
    }
}

public sealed class CreateTicketPriorityCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateTicketPriorityCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketPriorityCommand request, CancellationToken cancellationToken)
    {
        var entity = new TicketPriority
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            NameCs = request.NameCs,
            NameEn = request.NameEn,
            Color = request.Color,
            SortOrder = request.SortOrder,
            IsActive = true,
            IsDefault = request.IsDefault,
            ProjectTemplateId = request.ProjectTemplateId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TicketPriorities.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// UPDATE
public sealed record UpdateTicketPriorityCommand(Guid Id, string Name, string? NameCs, string? NameEn, string Color, int SortOrder, bool IsActive, bool IsDefault) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateTicketPriorityCommandValidator : AbstractValidator<UpdateTicketPriorityCommand>
{
    public UpdateTicketPriorityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);
    }
}

public sealed class UpdateTicketPriorityCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateTicketPriorityCommand>
{
    public async Task Handle(UpdateTicketPriorityCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TicketPriorities
            .FirstOrDefaultAsync(tp => tp.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TicketPriority), request.Id);

        entity.Name = request.Name;
        entity.NameCs = request.NameCs;
        entity.NameEn = request.NameEn;
        entity.Color = request.Color;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.IsDefault = request.IsDefault;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteTicketPriorityCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DeleteTicketPriorityCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteTicketPriorityCommand>
{
    public async Task Handle(DeleteTicketPriorityCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TicketPriorities
            .FirstOrDefaultAsync(tp => tp.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TicketPriority), request.Id);

        dbContext.TicketPriorities.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

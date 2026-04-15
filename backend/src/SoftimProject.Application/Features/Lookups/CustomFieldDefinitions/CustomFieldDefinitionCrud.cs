using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Lookups.CustomFieldDefinitions;

// DTO
public sealed record CustomFieldDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    string FieldType,
    bool IsRequired,
    string? Options,
    int SortOrder,
    bool IsActive);

// GET ALL
public sealed record GetCustomFieldDefinitionsQuery : IRequest<List<CustomFieldDefinitionDto>>;

public sealed class GetCustomFieldDefinitionsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCustomFieldDefinitionsQuery, List<CustomFieldDefinitionDto>>
{
    public async Task<List<CustomFieldDefinitionDto>> Handle(GetCustomFieldDefinitionsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.CustomFieldDefinitions
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new CustomFieldDefinitionDto(
                d.Id, d.Name, d.Description, d.FieldType.ToString(),
                d.IsRequired, d.Options, d.SortOrder, d.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateCustomFieldDefinitionCommand(
    string Name,
    string? Description,
    string FieldType,
    bool IsRequired,
    string? Options,
    int SortOrder) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class CreateCustomFieldDefinitionCommandValidator : AbstractValidator<CreateCustomFieldDefinitionCommand>
{
    public CreateCustomFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.FieldType).NotEmpty().Must(v => Enum.TryParse<CustomFieldType>(v, out _)).WithMessage("Invalid field type.");
        RuleFor(x => x.Options).MaximumLength(4000);
    }
}

public sealed class CreateCustomFieldDefinitionCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCustomFieldDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entity = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            FieldType = Enum.Parse<CustomFieldType>(request.FieldType),
            IsRequired = request.IsRequired,
            Options = request.Options,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.CustomFieldDefinitions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// UPDATE
public sealed record UpdateCustomFieldDefinitionCommand(
    Guid Id,
    string Name,
    string? Description,
    string FieldType,
    bool IsRequired,
    string? Options,
    int SortOrder,
    bool IsActive) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateCustomFieldDefinitionCommandValidator : AbstractValidator<UpdateCustomFieldDefinitionCommand>
{
    public UpdateCustomFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.FieldType).NotEmpty().Must(v => Enum.TryParse<CustomFieldType>(v, out _)).WithMessage("Invalid field type.");
        RuleFor(x => x.Options).MaximumLength(4000);
    }
}

public sealed class UpdateCustomFieldDefinitionCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateCustomFieldDefinitionCommand>
{
    public async Task Handle(UpdateCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.CustomFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomFieldDefinition), request.Id);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.FieldType = Enum.Parse<CustomFieldType>(request.FieldType);
        entity.IsRequired = request.IsRequired;
        entity.Options = request.Options;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteCustomFieldDefinitionCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DeleteCustomFieldDefinitionCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteCustomFieldDefinitionCommand>
{
    public async Task Handle(DeleteCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.CustomFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomFieldDefinition), request.Id);

        dbContext.CustomFieldDefinitions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.Companies;

// DTO
public sealed record CompanyDto(Guid Id, string Name, string? Description, bool IsActive);

// GET ALL
public sealed record GetCompaniesQuery : IRequest<List<CompanyDto>>;

public sealed class GetCompaniesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCompaniesQuery, List<CompanyDto>>
{
    public async Task<List<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Companies
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto(c.Id, c.Name, c.Description, c.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateCompanyCommand(string Name, string? Description) : IRequest<Guid>;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateCompanyCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCompanyCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);
        return company.Id;
    }
}

// UPDATE
public sealed record UpdateCompanyCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateCompanyCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateCompanyCommand>
{
    public async Task Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.Id);

        company.Name = request.Name;
        company.Description = request.Description;
        company.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DELETE
public sealed record DeleteCompanyCommand(Guid Id) : IRequest;

public sealed class DeleteCompanyCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteCompanyCommand>
{
    public async Task Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.Id);

        dbContext.Companies.Remove(company);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

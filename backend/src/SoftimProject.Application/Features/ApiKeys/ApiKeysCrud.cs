using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.ApiKeys;

// DTOs
public sealed record ApiKeyDto(
    Guid Id,
    string Name,
    string Prefix,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime? RevokedAt,
    DateTime CreatedAt);

// The plaintext key is returned ONLY here, once, at creation.
public sealed record GenerateApiKeyResult(
    Guid Id,
    string Name,
    string Prefix,
    string PlaintextKey,
    DateTime? ExpiresAt,
    DateTime CreatedAt);

// LIST (own keys)
public sealed record GetApiKeysQuery : IRequest<List<ApiKeyDto>>;

public sealed class GetApiKeysQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetApiKeysQuery, List<ApiKeyDto>>
{
    public async Task<List<ApiKeyDto>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");
        return await dbContext.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyDto(k.Id, k.Name, k.Prefix, k.ExpiresAt, k.LastUsedAt, k.RevokedAt, k.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// GENERATE
public sealed record GenerateApiKeyCommand(string Name, int? ExpiresInDays) : IRequest<GenerateApiKeyResult>;

public sealed class GenerateApiKeyCommandValidator : AbstractValidator<GenerateApiKeyCommand>
{
    public GenerateApiKeyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExpiresInDays).GreaterThan(0).LessThanOrEqualTo(3650)
            .When(x => x.ExpiresInDays.HasValue);
    }
}

public sealed class GenerateApiKeyCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GenerateApiKeyCommand, GenerateApiKeyResult>
{
    public async Task<GenerateApiKeyResult> Handle(GenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var plaintext = ApiKeyHasher.Generate();
        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            KeyHash = ApiKeyHasher.Hash(plaintext),
            Prefix = ApiKeyHasher.DisplayPrefix(plaintext),
            ExpiresAt = request.ExpiresInDays.HasValue
                ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
                : null,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.ApiKeys.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new GenerateApiKeyResult(entity.Id, entity.Name, entity.Prefix, plaintext, entity.ExpiresAt, entity.CreatedAt);
    }
}

// REVOKE (own key, or Admin)
public sealed record RevokeApiKeyCommand(Guid Id) : IRequest;

public sealed class RevokeApiKeyCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<RevokeApiKeyCommand>
{
    public async Task Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var key = await dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ApiKey), request.Id);

        if (key.UserId != userId && !currentUser.IsInRole("Admin"))
            throw new UnauthorizedAccessException("You can only revoke your own API keys.");

        if (key.RevokedAt is null)
        {
            key.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

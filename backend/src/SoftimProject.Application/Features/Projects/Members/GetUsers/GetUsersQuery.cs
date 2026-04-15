using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.Members.GetUsers;

public sealed record UserOptionDto(
    Guid Id,
    string DisplayName,
    string Email,
    string? AvatarUrl);

public sealed record GetUsersQuery : IRequest<List<UserOptionDto>>;

public sealed class GetUsersQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetUsersQuery, List<UserOptionDto>>
{
    public async Task<List<UserOptionDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserOptionDto(u.Id, u.DisplayName, u.Email, u.AvatarUrl))
            .ToListAsync(cancellationToken);
    }
}

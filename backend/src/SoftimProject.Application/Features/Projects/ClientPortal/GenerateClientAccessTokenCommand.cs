using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.ClientPortal;

public sealed record GenerateClientAccessTokenCommand(Guid ProjectId)
    : IRequest<string>, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class GenerateClientAccessTokenCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GenerateClientAccessTokenCommand, string>
{
    public async Task<string> Handle(GenerateClientAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        project.ClientAccessToken = GenerateToken();
        project.ClientAccessEnabled = true;

        await dbContext.SaveChangesAsync(cancellationToken);
        return project.ClientAccessToken;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

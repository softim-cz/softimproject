using Microsoft.AspNetCore.DataProtection;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services;

/// <summary>
/// <see cref="ISecretProtector"/> backed by ASP.NET Data Protection. The key ring is
/// managed by the framework (on Azure App Service persisted under the app's home dir);
/// for multi-instance durability the ring can later be backed by Blob/Key Vault without
/// changing this class.
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "IntegrationConnection.Secret.v1";
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector(Purpose);

    public string? Protect(string? plaintext)
        => string.IsNullOrEmpty(plaintext) ? null : _protector.Protect(plaintext);

    public string? Unprotect(string? ciphertext)
        => string.IsNullOrEmpty(ciphertext) ? null : _protector.Unprotect(ciphertext);
}

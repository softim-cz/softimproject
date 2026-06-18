namespace SoftimProject.Application.Interfaces;

// Profilová pole z firemního adresáře (Microsoft Entra / Graph), která nejsou
// běžně součástí přihlašovacího tokenu.
public sealed record DirectoryUserProfile(string? JobTitle, string? CompanyName);

// Doplnění uživatelského profilu z firemního adresáře. Implementace přes Graph je
// volitelná — bez nakonfigurovaného připojení se vrací null a provisioning běží dál.
public interface IUserDirectory
{
    Task<DirectoryUserProfile?> GetProfileAsync(string entraObjectId, CancellationToken cancellationToken = default);
}

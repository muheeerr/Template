using Template.Modules.Modules.Auth.Domain;
using Microsoft.AspNetCore.Identity;

namespace Template.Infrastructure.Authentication;

public sealed class CredentialHasher : ICredentialHasher
{
    private static readonly PasswordHasher<string> Hasher = new();

    public string Hash(string value) => Hasher.HashPassword(value, value);

    public bool Verify(string hash, string value)
    {
        var result = Hasher.VerifyHashedPassword(value, hash, value);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

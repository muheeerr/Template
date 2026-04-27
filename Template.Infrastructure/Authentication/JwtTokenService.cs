using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Template.Modules.Common.Authorization;
using Template.Modules.Common.Abstractions;
using Template.Modules.Modules.Auth.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Template.Infrastructure.Authentication;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock, ILogger<JwtTokenService> logger)
    {
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public AccessTokenDescriptor CreateAccessToken(AuthenticatedPrincipal principal)
    {
        _logger.LogInformation(
            "Issuing access token for user {UserId} with roles [{Roles}]",
            principal.UserId,
            string.Join(", ", principal.Roles));

        var expiresAt = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, principal.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, principal.Email),
            new("full_name", principal.FullName),
            new("token_version", principal.TokenVersion.ToString()),
            new(AppClaimTypes.TenantId, principal.TenantId.ToString()),
            new(ClaimTypes.NameIdentifier, principal.UserId.ToString()),
            new(ClaimTypes.Email, principal.Email)
        };

        claims.AddRange(principal.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(principal.Actions.Select(action => new Claim(AppClaimTypes.Action, action)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenDescriptor(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    public string CreateOpaqueToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}

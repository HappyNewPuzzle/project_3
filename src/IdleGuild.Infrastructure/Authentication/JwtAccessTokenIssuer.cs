using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdleGuild.Application.Abstractions.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace IdleGuild.Infrastructure.Authentication;

/// <summary>플레이어 ID를 subject로 갖는 서명된 게스트 JWT를 발급합니다.</summary>
public sealed class JwtAccessTokenIssuer(
    JwtOptions options,
    TimeProvider timeProvider) : IAccessTokenIssuer
{
    /// <summary>설정된 유효기간과 HMAC SHA-256 서명으로 액세스 토큰을 만듭니다.</summary>
    public AccessToken Issue(Guid playerId)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        options.Validate();

        var issuedAtUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = issuedAtUtc.AddMinutes(
            options.AccessTokenLifetimeMinutes);
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                playerId.ToString("D")),
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("D")),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(issuedAtUtc.UtcDateTime).ToString(),
                ClaimValueTypes.Integer64),
            new Claim("account_type", "guest")
        };
        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            issuedAtUtc.UtcDateTime,
            expiresAtUtc.UtcDateTime,
            signingCredentials);

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc);
    }
}

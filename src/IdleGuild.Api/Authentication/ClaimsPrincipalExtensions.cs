using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdleGuild.Api.Authentication;

/// <summary>검증된 JWT Claims에서 게임 플레이어 식별자를 안전하게 읽습니다.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>subject Claim이 유효한 Guid일 때만 플레이어 ID를 반환합니다.</summary>
    public static bool TryGetPlayerId(
        this ClaimsPrincipal principal,
        out Guid playerId)
    {
        var subject = principal.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(subject, out playerId);
    }
}

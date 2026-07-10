using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdleGuild.Api.Authorization;
using IdleGuild.Infrastructure.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace IdleGuild.Api.Tests;

/// <summary>관리자 API 테스트에 실제 서버 검증 규칙을 통과하는 서명 JWT를 제공합니다.</summary>
internal static class TestAdminTokenFactory
{
    public static string Create(JwtOptions options)
    {
        var now = DateTimeOffset.UtcNow;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                Guid.NewGuid().ToString("D")),
            new Claim(
                AdminAuthorization.AccountTypeClaim,
                AdminAuthorization.AdminAccountType)
        };
        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            now.UtcDateTime,
            now.AddMinutes(10).UtcDateTime,
            credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}

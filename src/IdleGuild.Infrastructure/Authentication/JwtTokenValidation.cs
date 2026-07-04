using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IdleGuild.Infrastructure.Authentication;

/// <summary>발급과 검증이 동일한 JWT 신뢰 기준을 사용하게 합니다.</summary>
public static class JwtTokenValidation
{
    /// <summary>서명·발급자·대상·만료를 모두 검사하는 검증 설정을 생성합니다.</summary>
    public static TokenValidationParameters Create(
        JwtOptions options)
    {
        options.Validate();

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}

namespace IdleGuild.Infrastructure.Authentication;

/// <summary>게스트 JWT 발급과 검증에 공통으로 사용하는 설정입니다.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; init; }

    /// <summary>약한 서명 키나 잘못된 만료 설정으로 서버가 시작되지 않게 합니다.</summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(SigningKey);

        if (System.Text.Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 UTF-8 bytes.");
        }

        if (AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be positive.");
        }
    }
}

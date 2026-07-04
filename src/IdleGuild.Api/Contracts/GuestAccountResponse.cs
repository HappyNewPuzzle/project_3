namespace IdleGuild.Api.Contracts;

/// <summary>새 게스트 계정의 식별자와 인증 정보를 반환합니다.</summary>
public sealed record GuestAccountResponse(
    Guid PlayerId,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);

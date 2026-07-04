namespace IdleGuild.Application.Accounts.CreateGuest;

/// <summary>생성된 게스트의 식별자와 첫 액세스 토큰을 반환합니다.</summary>
public sealed record CreateGuestAccountResult(
    Guid PlayerId,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);

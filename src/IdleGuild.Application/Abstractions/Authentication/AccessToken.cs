namespace IdleGuild.Application.Abstractions.Authentication;

/// <summary>클라이언트에 전달할 액세스 토큰과 만료 시각을 표현합니다.</summary>
public sealed record AccessToken(
    string Value,
    DateTimeOffset ExpiresAtUtc);

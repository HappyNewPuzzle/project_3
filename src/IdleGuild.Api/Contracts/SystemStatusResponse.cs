namespace IdleGuild.Api.Contracts;

/// <summary>클라이언트에 API 상태와 신뢰 가능한 서버 UTC 시각을 전달합니다.</summary>
public sealed record SystemStatusResponse(
    string Status,
    DateTimeOffset ServerTimeUtc);

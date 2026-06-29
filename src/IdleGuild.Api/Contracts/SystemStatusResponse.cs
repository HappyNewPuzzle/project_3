namespace IdleGuild.Api.Contracts;

public sealed record SystemStatusResponse(
    string Status,
    DateTimeOffset ServerTimeUtc);

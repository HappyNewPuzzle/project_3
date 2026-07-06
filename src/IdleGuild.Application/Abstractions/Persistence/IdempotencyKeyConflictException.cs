namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>하나의 멱등 키가 서로 다른 명령 내용에 재사용됐음을 나타냅니다.</summary>
public sealed class IdempotencyKeyConflictException(
    string message) : Exception(message);

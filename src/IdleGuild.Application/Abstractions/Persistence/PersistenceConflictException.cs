namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>동시 수정이나 고유 키 경합으로 저장을 다시 시도해야 함을 나타냅니다.</summary>
public sealed class PersistenceConflictException(
    string message,
    Exception innerException) :
    Exception(message, innerException);

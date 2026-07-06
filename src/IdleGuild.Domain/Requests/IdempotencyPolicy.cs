namespace IdleGuild.Domain.Requests;

/// <summary>상태 변경 요청에서 공통으로 사용하는 멱등 키 규칙을 정의합니다.</summary>
public static class IdempotencyPolicy
{
    public const int MaxKeyLength = 64;
}

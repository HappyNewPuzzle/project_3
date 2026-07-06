namespace IdleGuild.Domain.Stages;

/// <summary>스테이지 도전의 성공 또는 진행 규칙상 실패 이유를 표현합니다.</summary>
public enum StageChallengeOutcome
{
    Succeeded = 1,
    InsufficientPower = 2,
    AlreadyCompleted = 3,
    StageLocked = 4
}

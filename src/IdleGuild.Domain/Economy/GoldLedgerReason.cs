namespace IdleGuild.Domain.Economy;

/// <summary>골드 잔액이 변경된 게임 내 원인을 구분합니다.</summary>
public enum GoldLedgerReason
{
    IdleRewardClaim = 1,
    HeroUpgrade = 2,
    StageCheckpoint = 3
}

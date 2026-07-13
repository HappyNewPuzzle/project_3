using System;

// 서버의 스테이지 승패 결과를 클라이언트 전투 HUD에서 사용할 연출 데이터로 변환합니다.
public sealed class IdleGuildBattlePresentation
{
    // 현재 서버 API는 상세 전투 로그를 주지 않으므로 HP와 데미지는 화면 연출 전용 값입니다.
    private const int HeroMaxHealthValue = 100;

    public int Stage { get; private set; }
    public bool IsVictory { get; private set; }
    public int HeroMaxHealth { get; private set; }
    public int HeroHealthAfter { get; private set; }
    public int MonsterMaxHealth { get; private set; }
    public int MonsterHealthAfter { get; private set; }
    public int DamageToHero { get; private set; }
    public int DamageToMonster { get; private set; }
    public string StageLabel { get; private set; }
    public string ResultLabel { get; private set; }

    // 서버가 반환한 outcome만 승패 판정에 사용하고 나머지는 시각 연출 값으로 계산합니다.
    public static IdleGuildBattlePresentation Create(int stage, string outcome)
    {
        int normalizedStage = Math.Max(1, stage);
        bool isVictory = string.Equals(outcome, "succeeded", StringComparison.OrdinalIgnoreCase);
        int monsterMaxHealth = 50 + (normalizedStage - 1) * 20;
        int monsterHealthAfter = isVictory ? 0 : Math.Max(1, monsterMaxHealth / 3);
        int heroHealthAfter = isVictory ? HeroMaxHealthValue : 0;

        return new IdleGuildBattlePresentation
        {
            Stage = normalizedStage,
            IsVictory = isVictory,
            HeroMaxHealth = HeroMaxHealthValue,
            HeroHealthAfter = heroHealthAfter,
            MonsterMaxHealth = monsterMaxHealth,
            MonsterHealthAfter = monsterHealthAfter,
            DamageToHero = HeroMaxHealthValue - heroHealthAfter,
            DamageToMonster = monsterMaxHealth - monsterHealthAfter,
            StageLabel = "STAGE " + normalizedStage,
            ResultLabel = isVictory ? "VICTORY" : "DEFEAT"
        };
    }
}

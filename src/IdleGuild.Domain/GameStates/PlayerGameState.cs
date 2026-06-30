namespace IdleGuild.Domain.GameStates;

/// <summary>플레이어 한 명의 재화와 핵심 진행 상태를 표현합니다.</summary>
public sealed class PlayerGameState
{
    // EF Core가 데이터베이스 값을 채울 때 사용하는 전용 생성자입니다.
    private PlayerGameState()
    {
    }

    private PlayerGameState(Guid playerId, DateTimeOffset createdAtUtc)
    {
        PlayerId = playerId;
        Gold = 0;
        HeroLevel = 1;
        HighestStage = 1;
        CreatedAtUtc = createdAtUtc;
        LastIdleRewardClaimedAtUtc = createdAtUtc;
    }

    public Guid PlayerId { get; private set; }

    public long Gold { get; private set; }

    public int HeroLevel { get; private set; }

    public int HighestStage { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastIdleRewardClaimedAtUtc { get; private set; }

    // PostgreSQL의 xmin 시스템 열과 연결되어 동시 수정을 감지합니다.
    public uint Version { get; private set; }

    /// <summary>검증된 플레이어 식별자와 서버 시각으로 초기 게임 상태를 생성합니다.</summary>
    public static PlayerGameState Create(
        Guid playerId,
        DateTimeOffset createdAt)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        if (createdAt == default)
        {
            throw new ArgumentException(
                "Creation time must be provided.",
                nameof(createdAt));
        }

        return new PlayerGameState(
            playerId,
            createdAt.ToUniversalTime());
    }
}

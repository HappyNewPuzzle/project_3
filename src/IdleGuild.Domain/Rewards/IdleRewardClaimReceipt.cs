using IdleGuild.Domain.Requests;

namespace IdleGuild.Domain.Rewards;

/// <summary>동일 요청의 중복 지급을 막고 최초 결과를 재현하는 영수증입니다.</summary>
public sealed class IdleRewardClaimReceipt
{
    // EF Core가 PostgreSQL 행을 복원할 때 사용하는 전용 생성자입니다.
    private IdleRewardClaimReceipt()
    {
        IdempotencyKey = string.Empty;
    }

    private IdleRewardClaimReceipt(
        Guid playerId,
        string idempotencyKey,
        IdleRewardSettlement settlement)
    {
        PlayerId = playerId;
        IdempotencyKey = idempotencyKey;
        GoldAwarded = settlement.GoldAwarded;
        AccumulatedSeconds = settlement.AccumulatedSeconds;
        GoldBalanceAfter = settlement.GoldBalanceAfter;
        ClaimedAtUtc = settlement.ClaimedAtUtc;
    }

    public Guid PlayerId { get; private set; }

    public string IdempotencyKey { get; private set; }

    public long GoldAwarded { get; private set; }

    public int AccumulatedSeconds { get; private set; }

    public long GoldBalanceAfter { get; private set; }

    public DateTimeOffset ClaimedAtUtc { get; private set; }

    /// <summary>검증된 플레이어·멱등 키·정산 결과로 재사용 가능한 영수증을 생성합니다.</summary>
    public static IdleRewardClaimReceipt Create(
        Guid playerId,
        string idempotencyKey,
        IdleRewardSettlement settlement)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            idempotencyKey);
        ArgumentNullException.ThrowIfNull(settlement);

        if (idempotencyKey.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idempotencyKey),
                $"Idempotency key cannot exceed {IdempotencyPolicy.MaxKeyLength} characters.");
        }

        if (settlement.GoldAwarded < 0 ||
            settlement.GoldBalanceAfter < 0 ||
            settlement.AccumulatedSeconds < 0 ||
            settlement.AccumulatedSeconds >
            IdleRewardPolicy.MaxAccumulationSeconds ||
            settlement.ClaimedAtUtc == default)
        {
            throw new ArgumentException(
                "Idle reward settlement contains invalid values.",
                nameof(settlement));
        }

        return new IdleRewardClaimReceipt(
            playerId,
            idempotencyKey,
            settlement);
    }
}

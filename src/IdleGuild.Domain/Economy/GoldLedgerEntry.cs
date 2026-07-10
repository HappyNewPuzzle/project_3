using IdleGuild.Domain.Requests;

namespace IdleGuild.Domain.Economy;

/// <summary>플레이어 골드의 한 번의 증가 또는 감소를 변경 불가능한 원장으로 기록합니다.</summary>
public sealed class GoldLedgerEntry
{
    // EF Core가 PostgreSQL 행을 복원할 때 사용하는 전용 생성자입니다.
    private GoldLedgerEntry()
    {
        ReferenceId = string.Empty;
    }

    private GoldLedgerEntry(
        Guid entryId,
        Guid playerId,
        GoldLedgerReason reason,
        long balanceBefore,
        long amount,
        long balanceAfter,
        string referenceId,
        DateTimeOffset occurredAtUtc)
    {
        EntryId = entryId;
        PlayerId = playerId;
        Reason = reason;
        BalanceBefore = balanceBefore;
        Amount = amount;
        BalanceAfter = balanceAfter;
        ReferenceId = referenceId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid EntryId { get; private set; }

    public Guid PlayerId { get; private set; }

    public GoldLedgerReason Reason { get; private set; }

    public long BalanceBefore { get; private set; }

    public long Amount { get; private set; }

    public long BalanceAfter { get; private set; }

    public string ReferenceId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    /// <summary>잔액 등식과 참조 정보를 검증해 골드 변경 원장을 생성합니다.</summary>
    public static GoldLedgerEntry Create(
        Guid playerId,
        GoldLedgerReason reason,
        long balanceBefore,
        long amount,
        long balanceAfter,
        string referenceId,
        DateTimeOffset occurredAtUtc)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        if (balanceBefore < 0 || balanceAfter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(balanceBefore),
                "Gold balances must not be negative.");
        }

        if (amount == 0 ||
            checked(balanceBefore + amount) != balanceAfter)
        {
            throw new ArgumentException(
                "Gold amount must be non-zero and match both balances.",
                nameof(amount));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            referenceId);
        var normalizedReferenceId = referenceId.Trim();

        if (normalizedReferenceId.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceId),
                $"Reference ID cannot exceed {IdempotencyPolicy.MaxKeyLength} characters.");
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException(
                "Occurrence time must be provided.",
                nameof(occurredAtUtc));
        }

        return new GoldLedgerEntry(
            Guid.NewGuid(),
            playerId,
            reason,
            balanceBefore,
            amount,
            balanceAfter,
            normalizedReferenceId,
            occurredAtUtc.ToUniversalTime());
    }
}

using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.Requests;
using IdleGuild.Domain.Rewards;

namespace IdleGuild.Application.Rewards.ClaimIdleReward;

/// <summary>방치 보상을 멱등하게 정산하고 동시 저장 충돌을 재시도합니다.</summary>
public sealed class ClaimIdleRewardHandler(
    IPlayerGameStateRepository gameStateRepository,
    IIdleRewardClaimRepository claimRepository,
    IGoldLedgerRepository goldLedgerRepository,
    IGameUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MaxSaveAttempts = 3;

    /// <summary>같은 플레이어와 멱등 키에는 최초 지급 결과만 반환합니다.</summary>
    public async Task<ClaimIdleRewardResult?> HandleAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            idempotencyKey);
        var normalizedKey = idempotencyKey.Trim();

        if (normalizedKey.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idempotencyKey),
                $"Idempotency key cannot exceed {IdempotencyPolicy.MaxKeyLength} characters.");
        }

        // 재시도마다 시간이 늘어나 보상이 달라지지 않도록 한 요청의 서버 시각을 고정합니다.
        var claimedAtUtc = timeProvider.GetUtcNow();

        for (var attempt = 1;
             attempt <= MaxSaveAttempts;
             attempt++)
        {
            var existing = await claimRepository.FindAsync(
                playerId,
                normalizedKey,
                cancellationToken);

            if (existing is not null)
            {
                return FromReceipt(
                    existing,
                    isReplay: true);
            }

            var gameState =
                await gameStateRepository.FindForUpdateAsync(
                    playerId,
                    cancellationToken);

            if (gameState is null)
            {
                return null;
            }

            var settlement = gameState.ClaimIdleReward(
                claimedAtUtc);
            var receipt = IdleRewardClaimReceipt.Create(
                playerId,
                normalizedKey,
                settlement);
            claimRepository.Add(receipt);

            if (settlement.GoldAwarded > 0)
            {
                // 실제 지급된 골드를 영수증과 같은 트랜잭션의 감사 원장에 추가합니다.
                goldLedgerRepository.Add(
                    GoldLedgerEntry.Create(
                        playerId,
                        GoldLedgerReason.IdleRewardClaim,
                        settlement.GoldBalanceAfter -
                        settlement.GoldAwarded,
                        settlement.GoldAwarded,
                        settlement.GoldBalanceAfter,
                        normalizedKey,
                        settlement.ClaimedAtUtc));
            }

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return FromReceipt(
                    receipt,
                    isReplay: false);
            }
            catch (PersistenceConflictException)
                when (attempt < MaxSaveAttempts)
            {
                // 실패한 추적 상태를 제거한 뒤 최신 DB 상태와 영수증을 다시 읽습니다.
                unitOfWork.DiscardChanges();
            }
        }

        throw new InvalidOperationException(
            "Idle reward claim could not be saved after retries.");
    }

    private static ClaimIdleRewardResult FromReceipt(
        IdleRewardClaimReceipt receipt,
        bool isReplay) =>
        new(
            receipt.IdempotencyKey,
            receipt.GoldAwarded,
            receipt.AccumulatedSeconds,
            receipt.GoldBalanceAfter,
            receipt.RemainderHundredths,
            receipt.ProductionPercent,
            receipt.ClaimedAtUtc,
            isReplay);
}

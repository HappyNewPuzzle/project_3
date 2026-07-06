using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Requests;

namespace IdleGuild.Application.Heroes.UpgradeMainHero;

/// <summary>주 영웅 강화를 멱등하게 판정하고 저장 충돌을 재시도합니다.</summary>
public sealed class UpgradeMainHeroHandler(
    IPlayerGameStateRepository gameStateRepository,
    IHeroUpgradeReceiptRepository receiptRepository,
    IGameUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MaxSaveAttempts = 3;

    /// <summary>같은 플레이어와 멱등 키에는 최초 강화 판정만 반환합니다.</summary>
    public async Task<UpgradeMainHeroResult?> HandleAsync(
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

        // 충돌 재시도 전후에 처리 시각이 달라지지 않도록 요청 시각을 한 번만 읽습니다.
        var processedAtUtc = timeProvider.GetUtcNow();

        for (var attempt = 1;
             attempt <= MaxSaveAttempts;
             attempt++)
        {
            var existing = await receiptRepository.FindAsync(
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

            var settlement = gameState.UpgradeMainHero(
                processedAtUtc);
            var receipt = HeroUpgradeReceipt.Create(
                playerId,
                normalizedKey,
                settlement);
            receiptRepository.Add(receipt);

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
                // 최신 레벨·잔액·영수증을 다시 읽도록 실패한 추적 상태를 제거합니다.
                unitOfWork.DiscardChanges();
            }
        }

        throw new InvalidOperationException(
            "Hero upgrade could not be saved after retries.");
    }

    private static UpgradeMainHeroResult FromReceipt(
        HeroUpgradeReceipt receipt,
        bool isReplay) =>
        new(
            receipt.IdempotencyKey,
            receipt.Outcome,
            receipt.PreviousLevel,
            receipt.HeroLevelAfter,
            receipt.GoldCost,
            receipt.GoldBalanceAfter,
            receipt.ProcessedAtUtc,
            isReplay);
}

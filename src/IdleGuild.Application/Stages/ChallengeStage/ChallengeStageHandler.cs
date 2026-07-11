using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.Requests;
using IdleGuild.Domain.Stages;

namespace IdleGuild.Application.Stages.ChallengeStage;

/// <summary>스테이지 도전을 멱등하게 판정하고 저장 충돌을 재시도합니다.</summary>
public sealed class ChallengeStageHandler(
    IPlayerGameStateRepository gameStateRepository,
    IStageChallengeReceiptRepository receiptRepository,
    IPlayerEquipmentRepository equipmentRepository,
    IGoldLedgerRepository goldLedgerRepository,
    IGameUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MaxSaveAttempts = 3;

    /// <summary>같은 플레이어와 멱등 키에는 최초 스테이지 판정만 반환합니다.</summary>
    public async Task<ChallengeStageResult?> HandleAsync(
        Guid playerId,
        int targetStage,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        StageChallengePolicy.ValidateStage(targetStage);
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

        // 재시도 전후에 전투와 생산 체크포인트 시각이 바뀌지 않게 고정합니다.
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
                if (existing.TargetStage != targetStage)
                {
                    throw new IdempotencyKeyConflictException(
                        "Idempotency key was already used for a different stage.");
                }

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

            var equipped = await equipmentRepository
                .ListEquippedAsync(
                    playerId,
                    cancellationToken);
            var equipmentPowerBonus = EquipmentCatalog
                .CalculateEquippedPowerBonus(equipped);

            var settlement = gameState.ChallengeStage(
                targetStage,
                equipmentPowerBonus,
                processedAtUtc);
            var receipt = StageChallengeReceipt.Create(
                playerId,
                normalizedKey,
                settlement);
            receiptRepository.Add(receipt);

            if (settlement.CheckpointGoldAwarded > 0)
            {
                // 스테이지 성공 직전 정산된 골드를 별도 원인으로 원장에 기록합니다.
                goldLedgerRepository.Add(
                    GoldLedgerEntry.Create(
                        playerId,
                        GoldLedgerReason.StageCheckpoint,
                        settlement.GoldBalanceAfter -
                        settlement.CheckpointGoldAwarded,
                        settlement.CheckpointGoldAwarded,
                        settlement.GoldBalanceAfter,
                        normalizedKey,
                        settlement.ProcessedAtUtc));
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
                // 최신 진행 상태와 영수증으로 다시 판정하도록 변경 추적을 비웁니다.
                unitOfWork.DiscardChanges();
            }
        }

        throw new InvalidOperationException(
            "Stage challenge could not be saved after retries.");
    }

    private static ChallengeStageResult FromReceipt(
        StageChallengeReceipt receipt,
        bool isReplay) =>
        new(
            receipt.IdempotencyKey,
            receipt.TargetStage,
            receipt.Outcome,
            receipt.PreviousHighestStage,
            receipt.HighestStageAfter,
            receipt.HeroPower,
            receipt.RequiredPower,
            receipt.ProductionBonusPercentAfter,
            receipt.CheckpointGoldAwarded,
            receipt.GoldBalanceAfter,
            receipt.ProcessedAtUtc,
            isReplay);
}

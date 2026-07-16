using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.Requests;

namespace IdleGuild.Application.Equipment.ChangeEquipment;

/// <summary>장비 장착·해제를 멱등하게 처리하고 동시 슬롯 충돌을 재시도합니다.</summary>
public sealed class ChangeEquipmentHandler(
    IPlayerEquipmentRepository equipmentRepository,
    IEquipmentChangeReceiptRepository receiptRepository,
    IGameUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MaxSaveAttempts = 3;

    public async Task<ChangeEquipmentResult?> HandleAsync(
        Guid playerId,
        Guid equipmentId,
        bool desiredEquipped,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty ||
            equipmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player and equipment IDs must not be empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            idempotencyKey);
        var normalizedKey = idempotencyKey.Trim();

        if (normalizedKey.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idempotencyKey));
        }

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
                if (existing.EquipmentId != equipmentId ||
                    existing.DesiredEquipped != desiredEquipped)
                {
                    throw new IdempotencyKeyConflictException(
                        "Idempotency key was already used for a different equipment command.");
                }

                return FromReceipt(existing, isReplay: true);
            }

            var target = await equipmentRepository
                .FindForUpdateAsync(
                    playerId,
                    equipmentId,
                    cancellationToken);

            if (target is null)
            {
                return null;
            }

            Guid? replacedEquipmentId = null;
            var changed = false;

            if (desiredEquipped)
            {
                var equipped = await equipmentRepository
                    .FindEquippedForUpdateAsync(
                        playerId,
                        target.Slot,
                        cancellationToken);

                if (equipped is not null &&
                    equipped.EquipmentId != target.EquipmentId)
                {
                    equipped.SetEquipped(false);
                    replacedEquipmentId = equipped.EquipmentId;
                    // PostgreSQL의 장착 슬롯 부분 유니크 인덱스가 새 장비 UPDATE를
                    // 먼저 처리해 충돌하지 않도록 기존 장비 해제를 선반영합니다.
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                changed = target.SetEquipped(true) ||
                    replacedEquipmentId is not null;
            }
            else
            {
                changed = target.SetEquipped(false);
            }

            var receipt = EquipmentChangeReceipt.Create(
                playerId,
                normalizedKey,
                equipmentId,
                desiredEquipped,
                changed
                    ? EquipmentChangeOutcome.Succeeded
                    : EquipmentChangeOutcome
                        .AlreadyInDesiredState,
                replacedEquipmentId,
                processedAtUtc);
            receiptRepository.Add(receipt);

            try
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);
                return FromReceipt(receipt, isReplay: false);
            }
            catch (PersistenceConflictException)
                when (attempt < MaxSaveAttempts)
            {
                // 최신 장착 슬롯과 영수증을 다시 읽도록 실패한 EF 추적 상태를 제거합니다.
                unitOfWork.DiscardChanges();
            }
        }

        throw new InvalidOperationException(
            "Equipment change could not be saved after retries.");
    }

    private static ChangeEquipmentResult FromReceipt(
        EquipmentChangeReceipt receipt,
        bool isReplay) =>
        new(
            receipt.IdempotencyKey,
            receipt.EquipmentId,
            receipt.DesiredEquipped,
            receipt.Outcome,
            receipt.ReplacedEquipmentId,
            receipt.ProcessedAtUtc,
            isReplay);
}

using IdleGuild.Domain.Stages;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>스테이지 도전 영수증의 조회와 추가를 저장 기술과 분리합니다.</summary>
public interface IStageChallengeReceiptRepository
{
    Task<StageChallengeReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    void Add(StageChallengeReceipt receipt);
}

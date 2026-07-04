using IdleGuild.Domain.Rewards;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>방치 보상 영수증의 조회와 추가를 저장 기술과 분리합니다.</summary>
public interface IIdleRewardClaimRepository
{
    Task<IdleRewardClaimReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    void Add(IdleRewardClaimReceipt receipt);
}

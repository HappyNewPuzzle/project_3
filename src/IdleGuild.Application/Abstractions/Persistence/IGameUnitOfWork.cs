namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>한 유스케이스에서 발생한 영속성 변경을 원자적으로 저장합니다.</summary>
public interface IGameUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}

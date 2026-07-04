using IdleGuild.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IdleGuild.Infrastructure.Persistence;

/// <summary>EF Core 저장과 동시성 충돌 변환을 한 작업 단위로 제공합니다.</summary>
public sealed class EfGameUnitOfWork(
    GameDbContext dbContext) : IGameUnitOfWork
{
    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConflictException(
                "게임 상태가 다른 요청에서 먼저 변경되었습니다.",
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new PersistenceConflictException(
                "동일한 멱등 키의 정산 영수증이 이미 저장되었습니다.",
                exception);
        }
    }

    /// <summary>충돌한 변경 추적 상태를 비워 다음 시도를 새로 조회하게 합니다.</summary>
    public void DiscardChanges() =>
        dbContext.ChangeTracker.Clear();
}

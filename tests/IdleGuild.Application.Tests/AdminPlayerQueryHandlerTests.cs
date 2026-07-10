using IdleGuild.Application.Admin.Players.GetAdminPlayer;
using IdleGuild.Application.Admin.Players.GetGoldLedgerPage;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Application.Tests;

/// <summary>관리자 플레이어 상태와 골드 원장 키셋 조회를 DB 없이 검증합니다.</summary>
public sealed class AdminPlayerQueryHandlerTests
{
    // 관리자 상태 조회는 일반 클라이언트보다 생성 시각과 DB 버전까지 반환해야 합니다.
    [Fact]
    public async Task GetPlayer_WithExistingState_ReturnsOperationalFields()
    {
        var playerId = Guid.NewGuid();
        var createdAt = Utc(0);
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            createdAt));
        var handler = new GetAdminPlayerHandler(
            repository);

        var result = await handler.HandleAsync(playerId);

        Assert.NotNull(result);
        Assert.Equal(playerId, result.PlayerId);
        Assert.Equal(createdAt, result.CreatedAtUtc);
        Assert.Equal(0u, result.Version);
    }

    // 최신 두 행 뒤의 커서를 사용하면 중복 없이 남은 과거 행만 조회해야 합니다.
    [Fact]
    public async Task GetLedger_WithCursor_ReturnsNextPageWithoutDuplicates()
    {
        var playerId = Guid.NewGuid();
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            Utc(0)));
        repository.Add(CreateEntry(
            playerId,
            "entry-1",
            balanceBefore: 0,
            amount: 10,
            Utc(1)));
        repository.Add(CreateEntry(
            playerId,
            "entry-2",
            balanceBefore: 10,
            amount: 20,
            Utc(2)));
        repository.Add(CreateEntry(
            playerId,
            "entry-3",
            balanceBefore: 30,
            amount: -5,
            Utc(3)));
        var handler = new GetGoldLedgerPageHandler(
            repository,
            repository);

        var first = await handler.HandleAsync(
            playerId,
            pageSize: 2,
            before: null);
        var second = await handler.HandleAsync(
            playerId,
            pageSize: 2,
            first!.NextPosition);

        Assert.Equal(
            ["entry-3", "entry-2"],
            first.Items.Select(item => item.ReferenceId));
        Assert.NotNull(first.NextPosition);
        Assert.NotNull(second);
        Assert.Equal(
            ["entry-1"],
            second.Items.Select(item => item.ReferenceId));
        Assert.Null(second.NextPosition);
    }

    private static GoldLedgerEntry CreateEntry(
        Guid playerId,
        string referenceId,
        long balanceBefore,
        long amount,
        DateTimeOffset occurredAtUtc) =>
        GoldLedgerEntry.Create(
            playerId,
            amount > 0
                ? GoldLedgerReason.IdleRewardClaim
                : GoldLedgerReason.HeroUpgrade,
            balanceBefore,
            amount,
            checked(balanceBefore + amount),
            referenceId,
            occurredAtUtc);

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 11, hour, 0, 0, TimeSpan.Zero);
}

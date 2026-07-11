using IdleGuild.Domain.Economy;
using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Rewards;
using IdleGuild.Domain.Stages;
using IdleGuild.Domain.Shop;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence;

/// <summary>게임 영속 객체와 PostgreSQL 테이블 사이의 작업 공간을 제공합니다.</summary>
public sealed class GameDbContext(
    DbContextOptions<GameDbContext> options) :
    DbContext(options)
{
    public DbSet<PlayerGameState> PlayerGameStates =>
        Set<PlayerGameState>();

    public DbSet<GoldLedgerEntry> GoldLedgerEntries =>
        Set<GoldLedgerEntry>();

    public DbSet<PlayerEquipment> PlayerEquipment =>
        Set<PlayerEquipment>();

    public DbSet<EquipmentChangeReceipt>
        EquipmentChangeReceipts =>
        Set<EquipmentChangeReceipt>();

    public DbSet<IdleRewardClaimReceipt> IdleRewardClaimReceipts =>
        Set<IdleRewardClaimReceipt>();

    public DbSet<HeroUpgradeReceipt> HeroUpgradeReceipts =>
        Set<HeroUpgradeReceipt>();

    public DbSet<StageChallengeReceipt> StageChallengeReceipts =>
        Set<StageChallengeReceipt>();

    public DbSet<ShopPurchaseReceipt> ShopPurchaseReceipts =>
        Set<ShopPurchaseReceipt>();

    // 같은 어셈블리의 모든 엔티티 구성을 자동으로 적용합니다.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GameDbContext).Assembly);
    }
}

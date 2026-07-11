using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Requests;
using IdleGuild.Domain.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>모의 구매 영수증의 멱등 키, 금액과 플레이어 관계를 매핑합니다.</summary>
public sealed class ShopPurchaseReceiptConfiguration : IEntityTypeConfiguration<ShopPurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<ShopPurchaseReceipt> builder)
    {
        builder.ToTable("shop_purchase_receipts", table =>
        {
            table.HasCheckConstraint("ck_shop_purchase_receipts_price", "mock_price > 0");
            table.HasCheckConstraint("ck_shop_purchase_receipts_gold", "gold_awarded > 0 AND gold_balance_after >= gold_awarded");
        });
        builder.HasKey(receipt => receipt.PurchaseId).HasName("pk_shop_purchase_receipts");
        builder.Property(receipt => receipt.PurchaseId).HasColumnName("purchase_id").ValueGeneratedNever();
        builder.Property(receipt => receipt.PlayerId).HasColumnName("player_id");
        builder.Property(receipt => receipt.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(IdempotencyPolicy.MaxKeyLength);
        builder.Property(receipt => receipt.ProductId).HasColumnName("product_id").HasMaxLength(ShopCatalog.MaxProductIdLength);
        builder.Property(receipt => receipt.MockPrice).HasColumnName("mock_price");
        builder.Property(receipt => receipt.GoldAwarded).HasColumnName("gold_awarded");
        builder.Property(receipt => receipt.GoldBalanceAfter).HasColumnName("gold_balance_after");
        builder.Property(receipt => receipt.PurchasedAtUtc).HasColumnName("purchased_at_utc");
        builder.HasIndex(receipt => new { receipt.PlayerId, receipt.IdempotencyKey }).IsUnique().HasDatabaseName("ux_shop_purchase_receipts_player_key");
        builder.HasIndex(receipt => new { receipt.PlayerId, receipt.PurchasedAtUtc, receipt.PurchaseId }).HasDatabaseName("ix_shop_purchase_receipts_player_time");
        builder.HasOne<PlayerGameState>().WithMany().HasForeignKey(receipt => receipt.PlayerId).OnDelete(DeleteBehavior.Cascade);
    }
}

using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.GameStates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>플레이어 보유 장비와 슬롯당 하나의 장착 불변식을 PostgreSQL에 매핑합니다.</summary>
public sealed class PlayerEquipmentConfiguration :
    IEntityTypeConfiguration<PlayerEquipment>
{
    public void Configure(
        EntityTypeBuilder<PlayerEquipment> builder)
    {
        builder.ToTable("player_equipment", table =>
        {
            table.HasCheckConstraint(
                "ck_player_equipment_slot",
                "slot >= 1 AND slot <= 2");
        });

        builder.HasKey(item => item.EquipmentId)
            .HasName("pk_player_equipment");
        builder.Property(item => item.EquipmentId)
            .HasColumnName("equipment_id")
            .ValueGeneratedNever();
        builder.Property(item => item.PlayerId)
            .HasColumnName("player_id");
        builder.Property(item => item.DefinitionId)
            .HasColumnName("definition_id")
            .HasMaxLength(
                EquipmentCatalog.MaxDefinitionIdLength);
        builder.Property(item => item.Slot)
            .HasColumnName("slot");
        builder.Property(item => item.IsEquipped)
            .HasColumnName("is_equipped");
        builder.Property(item => item.AcquiredAtUtc)
            .HasColumnName("acquired_at_utc");
        builder.Property(item => item.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        // PostgreSQL 부분 유일 인덱스로 플레이어의 같은 슬롯에는 하나만 장착시킵니다.
        builder.HasIndex(item => new
        {
            item.PlayerId,
            item.Slot
        })
            .IsUnique()
            .HasFilter("is_equipped")
            .HasDatabaseName(
                "ux_player_equipment_equipped_slot");

        builder.HasIndex(item => item.PlayerId)
            .HasDatabaseName("ix_player_equipment_player");

        builder.HasOne<PlayerGameState>()
            .WithMany()
            .HasForeignKey(item => item.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

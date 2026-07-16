using IdleGuild.Domain.GameStates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>플레이어 게임 상태의 PostgreSQL 스키마와 무결성 제약을 정의합니다.</summary>
public sealed class PlayerGameStateConfiguration :
    IEntityTypeConfiguration<PlayerGameState>
{
    /// <summary>도메인 속성을 명시적인 테이블·열 이름과 제약조건에 연결합니다.</summary>
    public void Configure(
        EntityTypeBuilder<PlayerGameState> builder)
    {
        builder.ToTable("player_game_states", table =>
        {
            table.HasCheckConstraint(
                "ck_player_game_states_gold_non_negative",
                "gold >= 0");
            table.HasCheckConstraint(
                "ck_player_game_states_hero_level_positive",
                "hero_level >= 1");
            table.HasCheckConstraint(
                "ck_player_game_states_highest_stage_positive",
                "highest_stage >= 1 AND highest_stage <= 32");
            table.HasCheckConstraint(
                "ck_player_game_states_idle_reward_remainder",
                "idle_reward_remainder_hundredths >= 0 AND idle_reward_remainder_hundredths < 100");
        });

        builder.HasKey(state => state.PlayerId)
            .HasName("pk_player_game_states");

        builder.Property(state => state.PlayerId)
            .HasColumnName("player_id")
            .ValueGeneratedNever();

        builder.Property(state => state.Gold)
            .HasColumnName("gold")
            .IsRequired();

        builder.Property(state => state.HeroLevel)
            .HasColumnName("hero_level")
            .IsRequired();

        builder.Property(state => state.HighestStage)
            .HasColumnName("highest_stage")
            .IsRequired();

        builder.Property(state => state.AttackLevel).HasColumnName("attack_level").HasDefaultValue(1).IsRequired();
        builder.Property(state => state.AttackSpeedLevel).HasColumnName("attack_speed_level").HasDefaultValue(0).IsRequired();
        builder.Property(state => state.CriticalLevel).HasColumnName("critical_level").HasDefaultValue(0).IsRequired();
        builder.Property(state => state.PrestigeLevel).HasColumnName("prestige_level").HasDefaultValue(0).IsRequired();
        builder.Property(state => state.SoulStones).HasColumnName("soul_stones").HasDefaultValue(0).IsRequired();
        builder.Property(state => state.EquipmentTier).HasColumnName("equipment_tier").HasDefaultValue(0).IsRequired();
        builder.Property(state => state.EquipmentCount).HasColumnName("equipment_count").HasDefaultValue(0).IsRequired();
        builder.Property(state => state.UnlockedRegion).HasColumnName("unlocked_region").HasDefaultValue(0).IsRequired();
        builder.Property(state => state.SkillOneLevel).HasColumnName("skill_one_level").HasDefaultValue(1).IsRequired();
        builder.Property(state => state.SkillTwoLevel).HasColumnName("skill_two_level").HasDefaultValue(1).IsRequired();
        builder.Property(state => state.SkillThreeLevel).HasColumnName("skill_three_level").HasDefaultValue(1).IsRequired();

        builder.Property(state => state.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(state => state.LastIdleRewardClaimedAtUtc)
            .HasColumnName("last_idle_reward_claimed_at_utc")
            .IsRequired();

        builder.Property(
                state =>
                    state.IdleRewardRemainderHundredths)
            .HasColumnName(
                "idle_reward_remainder_hundredths")
            .HasDefaultValue(0)
            .IsRequired();

        // xmin은 행이 변경될 때 PostgreSQL이 자동 갱신하는 동시성 토큰입니다.
        builder.Property(state => state.Version)
            .HasColumnName("xmin")
            .IsRowVersion();
    }
}

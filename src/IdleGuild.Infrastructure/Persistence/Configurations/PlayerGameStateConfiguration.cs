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
                "highest_stage >= 1");
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

        builder.Property(state => state.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(state => state.LastIdleRewardClaimedAtUtc)
            .HasColumnName("last_idle_reward_claimed_at_utc")
            .IsRequired();

        // xmin은 행이 변경될 때 PostgreSQL이 자동 갱신하는 동시성 토큰입니다.
        builder.Property(state => state.Version)
            .HasColumnName("xmin")
            .IsRowVersion();
    }
}

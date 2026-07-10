using IdleGuild.Domain.Economy;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>골드 변경 원장의 추가를 저장 기술과 분리합니다.</summary>
public interface IGoldLedgerRepository
{
    void Add(GoldLedgerEntry entry);
}

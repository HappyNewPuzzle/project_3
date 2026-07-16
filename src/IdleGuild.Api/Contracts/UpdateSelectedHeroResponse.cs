namespace IdleGuild.Api.Contracts;

/// <summary>PostgreSQL에 저장된 현재 선택 영웅 ID를 반환합니다.</summary>
public sealed record UpdateSelectedHeroResponse(
    string SelectedHeroId);

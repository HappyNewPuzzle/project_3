namespace IdleGuild.Api.Contracts;

/// <summary>플레이어가 선택할 안정적인 서버 영웅 ID를 받습니다.</summary>
public sealed record UpdateSelectedHeroRequest(
    string? SelectedHeroId);

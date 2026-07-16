namespace IdleGuild.Domain.GameStates;

/// <summary>Unity enum 값과 분리된 안정적인 서버 영웅 ID를 정의합니다.</summary>
public static class SelectedHeroPolicy
{
    public const string DefaultHeroId = "girl";
    public const string BlackCatHeroId = "black_cat";
    public const string ClassicHeroId = "classic";
    public const int MaxHeroIdLength = 32;

    private static readonly IReadOnlySet<string> SupportedHeroIds =
        new HashSet<string>(StringComparer.Ordinal)
        {
            DefaultHeroId,
            BlackCatHeroId,
            ClassicHeroId
        };

    public static bool IsSupported(string? heroId) =>
        heroId is not null && SupportedHeroIds.Contains(heroId);

    public static string Validate(string heroId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(heroId);
        var normalized = heroId.Trim();
        return IsSupported(normalized)
            ? normalized
            : throw new ArgumentOutOfRangeException(
                nameof(heroId),
                "Selected hero ID is not supported.");
    }
}

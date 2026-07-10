using System.Globalization;
using System.Text;
using IdleGuild.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.WebUtilities;

namespace IdleGuild.Api.Endpoints;

/// <summary>내부 원장 위치를 클라이언트가 해석할 필요 없는 URL 안전 커서로 변환합니다.</summary>
internal static class AdminLedgerCursorCodec
{
    public static string Encode(
        GoldLedgerPagePosition position)
    {
        var value = string.Create(
            CultureInfo.InvariantCulture,
            $"{position.OccurredAtUtc.UtcDateTime.Ticks}:{position.EntryId:N}");
        return WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(value));
    }

    public static bool TryDecode(
        string? cursor,
        out GoldLedgerPagePosition? position)
    {
        position = null;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var value = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(cursor));
            var separatorIndex = value.IndexOf(':');

            if (separatorIndex <= 0 ||
                !long.TryParse(
                    value[..separatorIndex],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var utcTicks) ||
                !Guid.TryParseExact(
                    value[(separatorIndex + 1)..],
                    "N",
                    out var entryId) ||
                utcTicks < DateTimeOffset.MinValue.UtcTicks ||
                utcTicks > DateTimeOffset.MaxValue.UtcTicks)
            {
                return false;
            }

            position = new GoldLedgerPagePosition(
                new DateTimeOffset(
                    utcTicks,
                    TimeSpan.Zero),
                entryId);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

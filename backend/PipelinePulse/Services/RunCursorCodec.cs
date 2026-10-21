using System.Globalization;
using System.Text;

namespace PipelinePulse.Services;

internal static class RunCursorCodec
{
    public static string Encode(DateTime startedAt, string id)
    {
        var value = $"{startedAt.Ticks}:{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string cursor, out DateTime startedAt, out string id)
    {
        startedAt = default;
        id = string.Empty;

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var separatorIndex = value.IndexOf(':');
            if (separatorIndex <= 0
                || separatorIndex == value.Length - 1
                || !long.TryParse(
                    value.AsSpan(0, separatorIndex),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var ticks)
                || ticks < DateTime.MinValue.Ticks
                || ticks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            startedAt = new DateTime(ticks, DateTimeKind.Utc);
            id = value[(separatorIndex + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

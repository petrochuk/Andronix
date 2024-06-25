using Andronix.Core.Globalization;

namespace Andronix.Core.Extensions;

public static class StringExtensions
{
    private static readonly char[] Separators = new[] { ' ', '_', ':', '/', '\\' };

    public static DateTimeOffset ToDateTimeOffset(this string value, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));

        if (DateTime.TryParse(value, out var result))
            return result;

        value = value.Trim();
        if (value.Equals(Strings.Tomorrow, StringComparison.OrdinalIgnoreCase))
            return timeProvider.GetUtcNow().AddDays(1);

        var parts = value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            if (Strings.Next.ContainsKey(parts[0]))
            {
                if (Strings.Day.ContainsKey(parts[1]))
                    return timeProvider.GetUtcNow().AddDays(1);
                if (Strings.Week.ContainsKey(parts[1]))
                    return timeProvider.GetUtcNow().AddDays(7).StartOfWeek(DayOfWeek.Sunday);
                if (Strings.Month.ContainsKey(parts[1]))
                    return (new DateTimeOffset(timeProvider.GetUtcNow().Year, timeProvider.GetUtcNow().Month, 1, 0, 0, 0, TimeSpan.Zero)).AddMonths(1);
                if (Strings.Year.ContainsKey(parts[1]))
                    return (new DateTimeOffset(timeProvider.GetUtcNow().Year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero));

                throw new NotImplementedException();
            }
        }

        throw new NotImplementedException();
    }
}

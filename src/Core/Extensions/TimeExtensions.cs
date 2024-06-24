using Andronix.Core.Globalization;

namespace Andronix.Core.Extensions;

public static class TimeExtensions
{
    public static string ToRelativeSentence(this DateTimeOffset value, TimeProvider timeProvider)
    {
        var now = timeProvider.GetLocalNow();
        var timeDiff = now - value;

        if (timeDiff.TotalMinutes < 2)
            return "a moment ago";

        if (timeDiff.TotalMinutes < 10)
            return "a few minutes ago";

        if (timeDiff.TotalMinutes < 20)
            return "about 15 minutes ago";

        if (timeDiff.TotalMinutes < 45)
            return "about half hour ago";

        if (timeDiff.TotalMinutes < 75)
            return "about an hour ago";

        if (timeDiff.TotalHours < 24)
        {
            if (value.Day < now.Day)
            {
                if(5 < timeDiff.TotalHours)
                    return "yesterday";

                return "a few hours";
            }

            return now.ToTimeOfDay(momentInTime: true);
        }

        if (timeDiff.TotalDays < 2)
            return "yesterday";

        if (timeDiff.TotalDays < 5)
            return $"a few days ago";

        if (timeDiff.TotalDays < 10)
            return $"about a week ago";

        if (timeDiff.TotalDays < 20)
            return $"a few weeks ago";

        if (timeDiff.TotalDays < 45)
            return $"a month ago";

        if (timeDiff.TotalDays < 300)
            return $"a few month ago";

        if (timeDiff.TotalDays < 400)
            return $"a year ago";

        if (timeDiff.TotalDays < 1000)
            return $"a few years ago";

        return "A long time ago, in a galaxy far far away...";
    }

    public static string ToTimeOfDay(this DateTimeOffset dateTime, bool momentInTime = false)
    {
        if (dateTime.Hour < 3)
            return momentInTime ? "at night" : "night";
        if (dateTime.Hour < 6)
            return "early morning";
        if (dateTime.Hour < 12)
            return momentInTime ? "in the morning" : "morning";
        if (dateTime.Hour < 17)
            return momentInTime ? "in the afternoon" : "afternoon";
        if (dateTime.Hour < 22)
            return momentInTime ? "at night" : "night";

        return momentInTime ? "in the evening" : "evening";
    }

    public static bool RelativeEquals(this DateTimeOffset dateTime, string compareTo, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(compareTo))
            return false;

        DateTimeOffset startDateTime, endDateTime;
        compareTo = compareTo.Trim();

        if (DateTimeOffset.TryParse(compareTo, out var compareToDateTime))
        {
            startDateTime = compareToDateTime.Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else if (string.Compare(compareTo, "today", StringComparison.OrdinalIgnoreCase) == 0)
        {
            startDateTime = timeProvider.GetLocalNow().Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else if (string.Compare(compareTo, "yesterday", StringComparison.OrdinalIgnoreCase) == 0)
        {
            startDateTime = timeProvider.GetLocalNow().AddDays(-1).Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else if (string.Compare(compareTo, "tomorrow", StringComparison.OrdinalIgnoreCase) == 0)
        {
            startDateTime = timeProvider.GetLocalNow().AddDays(1).Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else
        {
            var parts = compareTo.Split(new[]{ ' ', '_', ':' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                // Split concatenated words
                if (compareTo.StartsWith("last", StringComparison.OrdinalIgnoreCase))
                    parts = new[] { "last", compareTo.Substring(4) };
                else if (compareTo.StartsWith("prev", StringComparison.OrdinalIgnoreCase))
                    parts = new[] { "prev", compareTo.Substring(4) };
                else if (compareTo.StartsWith("previous", StringComparison.OrdinalIgnoreCase))
                    parts = new[] { "previous", compareTo.Substring(8) };
                else if (compareTo.StartsWith("next", StringComparison.OrdinalIgnoreCase))
                    parts = new[] { "next", compareTo.Substring(4) };
                else if (compareTo.StartsWith("this", StringComparison.OrdinalIgnoreCase))
                    parts = new[] { "this", compareTo.Substring(4) };
                else
                    return false;
            }

            if (parts.Length == 2)
            {
                if (string.Compare(parts[0], "last", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "prev", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "previous", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (Strings.Day.ContainsKey(parts[1]))
                    {
                        startDateTime = timeProvider.GetLocalNow().AddDays(-1).Date;
                        endDateTime = startDateTime.AddDays(1);
                    }
                    else if (Strings.Week.ContainsKey(parts[1]))
                    {
                        startDateTime = timeProvider.GetLocalNow().AddDays(-7).StartOfWeek(DayOfWeek.Sunday);
                        endDateTime = startDateTime.AddDays(7);
                    }
                    else if (Strings.Month.ContainsKey(parts[1]))
                    {
                        startDateTime = (new DateTime(timeProvider.GetLocalNow().Year, timeProvider.GetLocalNow().Month, 1)).AddMonths(-1);
                        endDateTime = startDateTime.AddMonths(1);
                    }
                    else if (string.Compare(parts[1], "year", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = new DateTime(timeProvider.GetLocalNow().Year - 1, 1, 1);
                        endDateTime = new DateTime(timeProvider.GetLocalNow().Year, 1, 1);
                    }
                    else
                        return false;
                }
                else if (string.Compare(parts[0], "this", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (Strings.Day.ContainsKey(parts[1]))
                    {
                        startDateTime = timeProvider.GetLocalNow().Date;
                        endDateTime = startDateTime.AddDays(1);
                    }
                    else if (Strings.Week.ContainsKey(parts[1]))
                    {
                        startDateTime = timeProvider.GetLocalNow().StartOfWeek(DayOfWeek.Sunday);
                        endDateTime = startDateTime.AddDays(7);
                    }
                    else if (Strings.Month.ContainsKey(parts[1]))
                    {
                        startDateTime = (new DateTime(timeProvider.GetLocalNow().Year, timeProvider.GetLocalNow().Month, 1));
                        endDateTime = startDateTime.AddMonths(1);
                    }
                    else if (string.Compare(parts[1], "year", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = new DateTime(timeProvider.GetLocalNow().Year, 1, 1);
                        endDateTime = new DateTime(timeProvider.GetLocalNow().Year + 1, 1, 1);
                    }
                    else
                        return false;
                }
                else // TODO add Next
                            return false;
            }
            else if (2 < parts.Length)
            {
                if (string.Compare(parts[0], "last", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "prev", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "previous", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!parts.TryGetTimeSpan(1, out var timeSpan))
                        return false;

                    startDateTime = timeProvider.GetLocalNow().Subtract(timeSpan);
                    endDateTime = timeProvider.GetLocalNow();
                }
                else if (Strings.Older.ContainsKey(parts[0]))
                {
                    if (!parts.TryGetTimeSpan(1, out var timeSpan))
                        return false;

                    startDateTime = DateTimeOffset.MinValue;
                    endDateTime = timeProvider.GetLocalNow().Subtract(timeSpan);
                }
                else if (Strings.Older.ContainsKey($"{parts[0]} {parts[1]}"))
                {
                    if (!parts.TryGetTimeSpan(2, out var timeSpan))
                        return false;

                    startDateTime = DateTimeOffset.MinValue;
                    endDateTime = timeProvider.GetLocalNow().Subtract(timeSpan);
                }
                else if (Strings.Newer.ContainsKey(parts[0]))
                {
                    if (!parts.TryGetTimeSpan(1, out var timeSpan))
                        return false;

                    startDateTime = timeProvider.GetLocalNow().Subtract(timeSpan);
                    endDateTime = DateTimeOffset.MaxValue;
                }
                else if (Strings.Newer.ContainsKey($"{parts[0]} {parts[1]}"))
                {
                    if (!parts.TryGetTimeSpan(2, out var timeSpan))
                        return false;

                    startDateTime = timeProvider.GetLocalNow().Subtract(timeSpan);
                    endDateTime = DateTimeOffset.MaxValue;
                }
                else // TODO add Next
                    return false;
            }
            else
                return false;
        }

        return dateTime.ToLocalTime() >= startDateTime && dateTime.ToLocalTime() < endDateTime;
    }

    /// <summary>
    /// Returns time offset from strings such as "1 day", "two week(s)", "3 months", "4 years"
    /// </summary>
    /// <param name="parts"></param>
    /// <param name="startIndex"></param>
    /// <param name="timeSpan"></param>
    /// <returns></returns>
    public static bool TryGetTimeSpan(this string[] parts, int startIndex, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;

        long total = 1;
        int endIndex = parts.Length - 1;
        if (string.Compare(parts[parts.Length - 1], "ago", StringComparison.OrdinalIgnoreCase) == 0)
            endIndex--;

        if (startIndex < endIndex)
        {
            if (!parts.TryParseToLong(startIndex, endIndex, out total))
                return false;
        }

        if (Strings.Day.ContainsKey(parts[endIndex]))
        {
            timeSpan = TimeSpan.FromDays(total);
            return true;
        }
        else if (Strings.Week.ContainsKey(parts[endIndex]))
        {
            timeSpan = TimeSpan.FromDays(7 * total);
            return true;
        }
        else if (Strings.Month.ContainsKey(parts[endIndex]))
        {
            timeSpan = TimeSpan.FromDays(30 * total);
            return true;
        }
        else if (Strings.Year.ContainsKey(parts[endIndex]))
        {
            timeSpan = TimeSpan.FromDays(365 * total);
            return true;
        }

        return false;
    }

    public static DateTimeOffset StartOfWeek(this DateTimeOffset dateTime, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        return dateTime.AddDays(-1 * diff).Date;
    }

    public static string ToTimeOfYear(this DateTimeOffset dateTime)
    {
        if (dateTime.Month < 3)
            return "winter";
        if (dateTime.Month < 6)
            return "spring";
        if (dateTime.Month < 9)
            return "summer";
        if (dateTime.Month < 12)
            return "autumn";

        return "winter";
    }
}

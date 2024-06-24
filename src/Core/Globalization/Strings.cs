namespace Andronix.Core.Globalization;

public static class Strings
{
    public static Dictionary<string, string> Last = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, string> Older = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, string> Newer = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, string> Day = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, string> Week = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, string> Month = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, string> Year = new(StringComparer.OrdinalIgnoreCase);

    static Strings()
    {
        Last.Add("Last", "Last");
        Last.Add("Latest", "Last");
        Last.Add("Recent", "Last");
        Last.Add("Recently", "Last");
        Last.Add("Newest", "Last");
        Last.Add("New", "Last");

        Older.Add("Older then", "Older");
        Older.Add("Older than", "Older");
        Older.Add("Earlier then", "Older");
        Older.Add("Earlier than", "Older");

        Newer.Add("Less then", "Newer");
        Newer.Add("Less than", "Newer");
        Newer.Add("lt", "Newer");

        Day.Add("Day", "Day");
        Day.Add("Days", "Day");
        Day.Add("Day's", "Day");
        Day.Add("Day(s)", "Day");

        Week.Add("Week", "Week");
        Week.Add("Weeks", "Week");
        Week.Add("Week's", "Week");
        Week.Add("Week(s)", "Week");

        Month.Add("Month", "Month");
        Month.Add("Months", "Month");
        Month.Add("Month's", "Month");
        Month.Add("Month(s)", "Month");

        Year.Add("Year", "Year");
        Year.Add("Years", "Year");
        Year.Add("Year's", "Year");
        Year.Add("Year(s)", "Year");
    }

    public const string Tomorrow = "tomorrow";
}

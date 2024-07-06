namespace Andronix.Core;

public static class SpecialPath
{
    public static string AppData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Andronix");

    public static string UserSettings => Path.Combine(AppData, "userSettings.json");
}

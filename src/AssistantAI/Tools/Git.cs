using Andronix.Interfaces;
using System.ComponentModel;

namespace Andronix.AssistantAI.Tools;

public class Git : ISpecializedAssistant
{
    #region Constants
    
    #endregion

    #region Fields & Constructors

    public Git()
    {
    }

    #endregion

    #region AI Functions

    [Description("Opens folder in PowerShell which containts Git repository.")]
    private async Task<string> OpenFolder(
        [Description("Repository or folder name")]
        string name)
    {
        var startInfo = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", @"pwsh.exe"));
        startInfo.UseShellExecute = true;
        startInfo.CreateNoWindow = true;
        var r = System.Diagnostics.Process.Start(startInfo);
        return "Done";
    }

    #endregion
}

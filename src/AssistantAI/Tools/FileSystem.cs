using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Andronix.AssistantAI.Tools;

public class FileSystem
{
    #region AI Functions

    [Description("Appends text to a file.")]
    private async Task<string> AppendTextToFile(
        [Description("File name or full path"), Required]
        string fileName,
        [Description("Text to append"), Required]
        string text)
    {
        try
        {
            var fileStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using (var streamWriter = new StreamWriter(fileStream))
            {
                await streamWriter.WriteLineAsync(text);
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }

        return "Done";
    }

    [Description("Opens file in VSCode.")]
    private async Task<string> OneFileInVSCode(
        [Description("File name or full path"), Required]
        string fileName)
    {
        try
        {
            var startInfo = new ProcessStartInfo(@"code.cmd");
            startInfo.Arguments = $"\"{fileName}\"";
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            var r = System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception e)
        {
            return e.Message;
        }

        return "Done";
    }

    #endregion
}

using Andronix.Core;
using System.ComponentModel;

namespace Andronix.AssistantAI.Tools;

public class Notes
{
    #region AI Functions

    [Description("Assistant can take a notes as a reminder for future conversations")]
    private async Task<string> TakeNote(
        [Description("Note title")]
        string title,
        [Description("Note content")]
        string content)
    {
        try
        {
            var fileStream = new FileStream(SpecialPath.AssistantNotes, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using (var streamWriter = new StreamWriter(fileStream))
            {
                await streamWriter.WriteLineAsync();
                await streamWriter.WriteLineAsync($"## {title}");
                await streamWriter.WriteLineAsync(content);
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }

        return "Done";
    }

    #endregion

}

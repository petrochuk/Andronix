using Andronix.Core.Extensions;
using Andronix.Interfaces;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using System.ComponentModel;

namespace Andronix.AssistantAI.Tools;

public class Outlook : ISpecializedAssistant
{
    GraphServiceClient _graphClient;

    public Outlook(GraphServiceClient graphClient)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    #region AI Functions

    [Description("Creates draft email user can review and send")]
    private async Task<string> CreateDraftEmail(
        [Description("Person name, email or alias to send email to")]
        string personName,
        [Description("Title")]
        string title,
        [Description("Content (can be in html)")]
        string content)
    {
        var emailMessage = new Message
        {
            Subject = title,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = content
            }
        };

        if (!string.IsNullOrWhiteSpace(personName))
        {
            var graphResponse = await _graphClient.FindPerson(personName);
            emailMessage.ToRecipients =
            [
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = graphResponse.EmailAddresses.FirstOrDefault()?.Address
                    }
                }
            ];
        }

        emailMessage = await _graphClient.Me.Messages.PostAsync(emailMessage);

        var startInfo = new ProcessStartInfo(emailMessage.WebLink);
        startInfo.UseShellExecute = true;
        startInfo.CreateNoWindow = true;
        var p = System.Diagnostics.Process.Start(startInfo);

        return "Email draft created";
    }

    #endregion
}

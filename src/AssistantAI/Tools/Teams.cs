using Andronix.Core.Extensions;
using Andronix.Interfaces;
using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using System.ComponentModel;

namespace Andronix.AssistantAI.Tools;

public class Teams : ISpecializedAssistant
{
    class SendChatRequest
    {
        public required GraphServiceClient GraphClient { get; set; }

        public required ChatMessage ChatMessage { get; set; }

        public async Task<bool> ChatCallback(Chat chat)
        {
            IsSuccessful = true;

            var chatMessage = await GraphClient.Me.Chats[chat.Id].Messages.PostAsync(ChatMessage);

            return false;
        }

        public bool IsSuccessful { get; set; }
    }

    #region Constants

    #endregion

    #region Fields & Constructors

    private GraphServiceClient _graphClient;
    Core.Options.TeamsAssistant _options;

    public Teams(GraphServiceClient graphClient, IOptions<Core.Options.TeamsAssistant> options)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    #endregion

    #region AI Functions

    [Description("Sends new chat or reply on Microsoft Teams chat or channel")]
    private async Task<string> SendTeamsMessage(
        [Description("Person name, email or alias to send message to")]
        string personName,
        [Description("Channel name")]
        string channelName,
        [Description("Chat message text")]
        string text)
    {
        var graphResponse = await _graphClient.FindPerson(personName);
        if (graphResponse == null)
            return $"Person {personName} not found";
        var person = new Andronix.Core.Model.Person(graphResponse);

        var oneOnOneChat = await _graphClient.Me.Chats.GetAsync(c =>
        {
            c.QueryParameters.Expand = ["members"];
            c.QueryParameters.Filter = $"chatType eq 'oneOnOne' and members/any(o: o/microsoft.graph.aadUserConversationMember/userId eq '{person.GraphId}')";
        });
        var sendChatRequest = new SendChatRequest
        {
            GraphClient = _graphClient,
            ChatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    Content = text,
                },
            },
        };
        var pageIterator = PageIterator<Chat, ChatCollectionResponse>.CreatePageIterator(_graphClient, oneOnOneChat, sendChatRequest.ChatCallback);
        await pageIterator.IterateAsync();

        return sendChatRequest.IsSuccessful ? "Message sent" : "Failed to send the message";
    }

    #endregion
}

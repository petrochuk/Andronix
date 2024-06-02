using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Text.RegularExpressions;

namespace Andronix.AssistantAI;

public partial class IntelligenceGatherer
{
    private GraphServiceClient _graphClient;
    ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    Thread _thread;

    public IntelligenceGatherer(IAuthenticationProvider authenticationProvider)
    {
        _thread = new Thread(DoWork);
        _graphClient = new GraphServiceClient(authenticationProvider);
    }

    public void Start()
    {
        _shutdownEvent.Reset();
        _thread.Start();
    }

    public void Stop() 
    {
        _shutdownEvent.Set();
        _thread.Join();
    }

    public void DoWork()
    {
        while (!_shutdownEvent.WaitOne(0))
        {
            ReadAllChats();
        }
    }

    private void ReadAllChats()
    {
        var chats = _graphClient.Me.Chats.GetAsync().Result;
        if (chats == null || chats.Value == null)
            return;

        var pageIterator = PageIterator<Chat, ChatCollectionResponse>.CreatePageIterator(_graphClient, chats, ReadChat);
        pageIterator.IterateAsync().Wait();
        if (_shutdownEvent.WaitOne(0))
            return;
    }

    private bool ReadChat(Chat chat)
    {
        Debug.WriteLine($"Topic: {chat.Topic}");

        var chatMessages = _graphClient.Me.Chats[chat.Id].Messages.GetAsync((c) =>
        {
            c.QueryParameters.Orderby = ["createdDateTime desc"];
        }).Result;
        if (chatMessages == null || chatMessages.Value == null)
            return !_shutdownEvent.WaitOne(0);

        var pageIterator = PageIterator<ChatMessage, ChatMessageCollectionResponse>.CreatePageIterator(_graphClient, chatMessages, ReadChatMessage);
        pageIterator.IterateAsync().Wait();

        return !_shutdownEvent.WaitOne(0);
    }

    private bool ReadChatMessage(ChatMessage chatMessage)
    {
        if (chatMessage.MessageType != ChatMessageType.Message)
            return !_shutdownEvent.WaitOne(0);

        Debug.Write($"{chatMessage.From.User.DisplayName}: ");
        Debug.WriteLine($"{StripHTML(chatMessage.Body.Content)}");

        Thread.Sleep(500);

        return !_shutdownEvent.WaitOne(0);
    }

    public static string StripHTML(string input)
    {
        return StripHtmlRegEx().Replace(input, String.Empty).Replace("&nbsp;", " ").Trim();
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex StripHtmlRegEx();
}

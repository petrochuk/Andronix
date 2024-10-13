using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Text.RegularExpressions;

namespace Andronix.AssistantAI;

public partial class KnowledgeCollectorBase
{
    private GraphServiceClient _graphClient;
    ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    Thread _thread;
    Team? _currentTeam;

    public KnowledgeCollectorBase(IAuthenticationProvider authenticationProvider)
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
        if (!_shutdownEvent.WaitOne(0) && _thread.IsAlive)
        {
            _shutdownEvent.Set();
            _thread.Join();
        }
    }

    public void DoWork()
    {
        /*
        while (!_shutdownEvent.WaitOne(0))
        {
            ReadAllTeamsChannels();
        }

        while (!_shutdownEvent.WaitOne(0))
        {
            ReadAllChats();
        }
        */
    }

    private void ReadAllTeamsChannels()
    {
        var channels = _graphClient.Me.JoinedTeams.GetAsync().Result;
        if (channels == null || channels.Value == null)
            return;

        var pageIterator = PageIterator<Team, TeamCollectionResponse>.CreatePageIterator(_graphClient, channels, ReadTeam);
        pageIterator.IterateAsync().Wait();
        if (_shutdownEvent.WaitOne(0))
            return;
    }

    private bool ReadTeam(Team team)
    {
        Debug.WriteLine($"Team: {team.DisplayName}");

        var channels = _graphClient.Teams[team.Id].Channels.GetAsync().Result;
        if (channels == null || channels.Value == null)
            return !_shutdownEvent.WaitOne(0);

        _currentTeam = team;
        var pageIterator = PageIterator<Channel, ChannelCollectionResponse>.CreatePageIterator(_graphClient, channels, ReadChannel);
        pageIterator.IterateAsync().Wait();

        return !_shutdownEvent.WaitOne(0);
    }

    private bool ReadChannel(Channel channel)
    {
        if (_currentTeam == null)
            return false;

        Debug.WriteLine($"Channel: {channel.DisplayName}");

        // Need ChannelMessage.Read.All or ChannelMessage.ReadWrite
        var channelMessages = _graphClient.Teams[_currentTeam.Id].Channels[channel.Id].Messages.GetAsync((c) =>
        {
            //c.QueryParameters.Orderby = ["createdDateTime desc"];
        }).Result;
        if (channelMessages == null || channelMessages.Value == null)
            return !_shutdownEvent.WaitOne(0);

        var pageIterator = PageIterator<ChatMessage, ChatMessageCollectionResponse>.CreatePageIterator(_graphClient, channelMessages, ReadChatMessage);
        pageIterator.IterateAsync().Wait();

        return !_shutdownEvent.WaitOne(0);
    }

    private void ReadAllChats()
    {
        var chats = _graphClient.Me.Chats.GetAsync((c) => 
        {
            c.QueryParameters.Orderby = ["lastMessagePreview/createdDateTime desc"];
            c.QueryParameters.Filter = "topic eq 'Building 7-ake'";
        }).Result;
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

        Thread.Sleep(400);

        return !_shutdownEvent.WaitOne(0);
    }

    List<string> _chatMessages = new List<string>();

    private bool ReadChatMessage(ChatMessage chatMessage)
    {
        if (chatMessage.MessageType != ChatMessageType.Message)
            return !_shutdownEvent.WaitOne(0);

        // Skips messages from applications
        if (chatMessage.From == null || chatMessage.From.User == null)
            return !_shutdownEvent.WaitOne(0);

        Debug.Write($"{chatMessage.From.User.DisplayName}: ");
        Debug.WriteLine($"{StripHTML(chatMessage.Body.Content)}");

        _chatMessages.Insert(0, $"{chatMessage.From.User.DisplayName}: {StripHTML(chatMessage.Body.Content)}");

        Thread.Sleep(100);

        return !_shutdownEvent.WaitOne(0);
    }

    public static string StripHTML(string input)
    {
        return StripHtmlRegEx().Replace(input, String.Empty).Replace("&nbsp;", " ").Trim();
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex StripHtmlRegEx();
}

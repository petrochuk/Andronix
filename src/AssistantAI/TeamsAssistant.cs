using Andronix.Interfaces;
using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Andronix.AssistantAI;

public class TeamsAssistant : IBackgroundWorker
{
    private GraphServiceClient _graphClient;
    ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    Thread _thread;
    Team? _team;
    Channel? _channel;
    Core.Options.TeamsAssistant _options;

    public TeamsAssistant(GraphServiceClient graphClient, IOptions<Core.Options.TeamsAssistant> options)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _thread = new Thread(DoWork);
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

    private async void DoWork()
    {
        var response = _graphClient.Me.JoinedTeams.GetAsync(o =>
        {
            o.QueryParameters.Filter = $"displayName eq '{Uri.EscapeDataString(_options.TeamName)}'";
        }).Result;

        if (response == null || response.Value == null)
        {
            Debug.WriteLine("No teams found");
            return;
        }

        _team = response.Value.FirstOrDefault(t => t.DisplayName!.Equals(_options.TeamName, StringComparison.OrdinalIgnoreCase));
        if (_team == null)
        {
            Debug.WriteLine($"Team '{_options.TeamName}' not found");
            return;
        }

        var channelsResponse = _graphClient.Teams[_team.Id].Channels.GetAsync(o =>
        {
            o.QueryParameters.Filter = $"displayName eq '{Uri.EscapeDataString(_options.ChannelName)}'";
        }).Result;
        if (channelsResponse == null || channelsResponse.Value == null)
        {
            Debug.WriteLine("No channels found");
            return;
        }
        _channel = channelsResponse.Value.FirstOrDefault(c => c.DisplayName!.Equals(_options.ChannelName, StringComparison.OrdinalIgnoreCase));
        if (_channel == null)
        {
            Debug.WriteLine($"Channel '{_options.ChannelName}' not found");
            return;
        }

        // Read delta changes
        var messagesResponse = _graphClient.Teams[_team.Id].Channels[_channel.Id].Messages.Delta.GetAsDeltaGetResponseAsync(o =>
        {
            o.QueryParameters.Expand = ["replies"];
        }).Result;
        var pageIterator = PageIterator<ChatMessage, Microsoft.Graph.Beta.Teams.Item.Channels.Item.Messages.Delta.DeltaGetResponse>.CreatePageIterator(_graphClient, messagesResponse!, ReadChatMessage);
        await pageIterator.IterateAsync();

        while (pageIterator.State != PagingState.Complete && !_shutdownEvent.WaitOne(0))
        {
            await Task.Delay(5000);
            await pageIterator.ResumeAsync();
        }
    }

    private async Task<bool> ReadChatMessage(ChatMessage chatMessage)
    {
        // Skip deleted messages
        if (chatMessage.DeletedDateTime != null)
            return !_shutdownEvent.WaitOne(0);

        // Skip messages with replies
        if (chatMessage.Replies != null && chatMessage.Replies.Count != 0)
            return !_shutdownEvent.WaitOne(0);

        var chatMessageToSend = new ChatMessage
        {
            Body = new ItemBody
            {
                Content = "Ack!",
            },
        };

        var chatMessageResponse = await _graphClient.Teams[_team.Id].Channels[_channel.Id].Messages[chatMessage.Id].Replies.PostAsync(chatMessageToSend);
        if (chatMessage.Replies == null)
            chatMessage.Replies = new List<ChatMessage>();
        chatMessage.Replies.Add(chatMessageResponse);

        return !_shutdownEvent.WaitOne(0);
    }
}

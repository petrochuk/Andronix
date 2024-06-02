using Andronix.Authentication;
using Andronix.Core;
using Andronix.Interfaces;
using Azure;
using Azure.AI.OpenAI.Assistants;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Andronix.AssistantAI;

public class Assistant
{
    #region Fields & Constructors

    private readonly IDialogPresenter _dialogPresenter;
    private Lazy<GraphServiceClient> _graphClient;
    private AssistantsClient _assistantClient;
    private CognitiveOptions _cognitiveOptions;
    private AssistantOptions _assistantOptions;
    private Lazy<UserSettings> _userSettings;
    private Azure.AI.OpenAI.Assistants.Assistant? _openAiAssistant;
    private Azure.AI.OpenAI.Assistants.AssistantThread? _openAiAssistantThread;

    public Assistant(
        IDialogPresenter dialogPresenter,
        IOptions<CognitiveOptions> cognitiveOptions, 
        IOptions<AssistantOptions> assistantOptions,
        AndronixTokenCredential andronixTokenCredential,
        IAuthenticationProvider authenticationProvider) 
    {
        _dialogPresenter = dialogPresenter ?? throw new ArgumentNullException(nameof(dialogPresenter));

        // Options and settings
        _cognitiveOptions = cognitiveOptions.Value ?? throw new ArgumentNullException(nameof(cognitiveOptions));
        _assistantOptions = assistantOptions.Value ?? throw new ArgumentNullException(nameof(assistantOptions));
        _userSettings = new Lazy<UserSettings>(() => {
            // Read from local JSON file
            var userSettings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_assistantOptions.UserSettings));
            if (userSettings == null)
                throw new InvalidOperationException($"Failed to read user settings from {_assistantOptions.UserSettings}");
            return userSettings;
        });

        // Clients
        _assistantClient = new AssistantsClient(_cognitiveOptions.EndPoint, andronixTokenCredential);
        _graphClient = new Lazy<GraphServiceClient>(() =>
        {
            var graphClient = new GraphServiceClient(authenticationProvider);
            return graphClient;
        });
    }

    #endregion

    #region Management

    public async Task CreateAssistantAsync()
    {
        if (!string.IsNullOrWhiteSpace(_userSettings.Value.AssistantId))
        {
            try
            {
                _dialogPresenter.UpdateStatus("Loading assistant...");
                var response = await _assistantClient.GetAssistantAsync(_userSettings.Value.AssistantId);
                _openAiAssistant = response.Value;
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                // Assistant not found
                _userSettings.Value.AssistantId = string.Empty;
            }
        }

        _dialogPresenter.UpdateStatus("Creating assistant...");
        var creationOptions = new AssistantCreationOptions("gpt-4o")
        {
            Name = _assistantOptions.Name,
        };

        var instructions = new StringBuilder();
        if (string.IsNullOrWhiteSpace(_assistantOptions.Instructions))
            instructions.AppendLine(@"You are a personal assistant. Your goal is to assist with saving time. 
                Please, use strategic thinking to provide only truly important information which is relevant to make best possible decisions.
                Only use facts, avoid opinions unless asked, and avoid stating obvious things. If you are not sure ask clarifying questions.");
        else
            instructions.AppendLine(File.ReadAllText(_assistantOptions.Instructions));

        if (_cognitiveOptions.KnowledgeFiles != null && _cognitiveOptions.KnowledgeFiles.Any())
        {
            instructions.AppendLine($"# Start of useful knowledge from files");
            foreach (var file in _cognitiveOptions.KnowledgeFiles)
            {
                instructions.AppendLine($"## Start of {Path.GetFileName(file)}");
                instructions.AppendLine(File.ReadAllText(file));
                instructions.AppendLine($"## End of {Path.GetFileName(file)}");
            }
            instructions.AppendLine($"# End of useful knowledge from files");
        }

        creationOptions.Instructions = instructions.ToString();
        var createResponse = await _assistantClient.CreateAssistantAsync(creationOptions);
        _userSettings.Value.AssistantId = createResponse.Value.Id;

        // Save settings to file
        using (var fileStream = File.Create(_assistantOptions.UserSettings))
        {
            await JsonSerializer.SerializeAsync(fileStream, _userSettings.Value);
        }

        _openAiAssistant = createResponse.Value;
    }

    public async Task StartNewThreadAsync()
    {
        if (_openAiAssistant == null)
            throw new InvalidOperationException("Assistant not created.");

        _dialogPresenter.UpdateStatus("Creating thread...");
        var threadOptions = new AssistantThreadCreationOptions();
        var createThreadResponse = await _assistantClient.CreateThreadAsync(threadOptions);
        if (createThreadResponse == null)
            throw new InvalidOperationException("Failed to create thread.");

        _openAiAssistantThread = createThreadResponse.Value;

        _dialogPresenter.UpdateStatus("Ready");
    }

    #endregion

    #region Chat

    public async Task SendPrompt(string prompt)
    {
        if (_openAiAssistant == null)
            throw new InvalidOperationException("Assistant not created.");
        if (_openAiAssistantThread == null)
            throw new InvalidOperationException("Thread not created.");

        _dialogPresenter.UpdateStatus("Sending prompt...");
        await _assistantClient.CreateMessageAsync(_openAiAssistantThread.Id, MessageRole.User, prompt);
        var runResponse = await _assistantClient.CreateRunAsync(
            _openAiAssistantThread.Id,
            new CreateRunOptions(_openAiAssistant.Id));
        
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        do
        {
            _dialogPresenter.UpdateStatus($"Waiting for response...{stopWatch.Elapsed:mm\\:ss}");
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            runResponse = await _assistantClient.GetRunAsync(_openAiAssistantThread.Id, runResponse.Value.Id);
        }
        while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);

        var afterRunMessagesResponse = await _assistantClient.GetMessagesAsync(_openAiAssistantThread.Id);
        var messages = afterRunMessagesResponse.Value.Data;

        var dialogHtml = new StringBuilder();
        dialogHtml.Append("<html><body style='font-family: Consolas; font-size: 14px;'>");

        _dialogPresenter.UpdateStatus("Displaying response...");
        foreach (var threadMessage in messages.Reverse())
        {
            Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
            foreach (MessageContent contentItem in threadMessage.ContentItems)
            {
                if (contentItem is MessageTextContent textItem)
                {
                    if (threadMessage.Role == MessageRole.User)
                        dialogHtml.Append($"<div style='color: blue;'>{textItem.Text.Replace("\n", "<br />")}</div>");
                    else if (threadMessage.Role == MessageRole.Assistant)
                        dialogHtml.Append($"<div style='color: green;'>Assistant: {textItem.Text.Replace("\n", "<br />")}</div>");
                    else
                        dialogHtml.Append($"<div style='color: black;'>{textItem.Text.Replace("\n", "<br />")}</div>");
                }
                else if (contentItem is MessageImageFileContent imageFileItem)
                {
                    Debug.WriteLine($"<image from ID: {imageFileItem.FileId}");
                }
            }
        }

        _dialogPresenter.ShowDialog(dialogHtml.ToString());
        _dialogPresenter.UpdateStatus("Ready");
    }

    #endregion

    public async Task GetMeAsync()
    {
        var user = await _graphClient.Value.Me.GetAsync();
        if (user == null)
            throw new InvalidOperationException("Failed to get user details.");
    }
}

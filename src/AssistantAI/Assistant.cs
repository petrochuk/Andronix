#region Usings
using Andronix.Authentication;
using Andronix.Core;
using Andronix.Core.Extensions;
using Andronix.Interfaces;
using Azure.AI.OpenAI;
using Markdig;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.VisualStudio.Services.Common;
using OpenAI.Assistants;
using OpenAI.Files;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
#endregion

namespace Andronix.AssistantAI;

public class Assistant
{
    #region Fields & Constructors

    private readonly IDialogPresenter _dialogPresenter;
    private Lazy<GraphServiceClient> _graphClient;
    private AzureOpenAIClient _azureOpenAIClient;
    private AssistantClient _assistantClient;
    private Core.Options.Cognitive _cognitiveOptions;
    private Core.Options.Assistant _assistantOptions;
    private Lazy<Core.UserSettings> _userSettings;
    private OpenAI.Assistants.Assistant? _openAiAssistant;
    private OpenAI.Assistants.AssistantThread? _openAiAssistantThread;
    private OpenAI.Files.FileClient _fileClient;
    private readonly Dictionary<string, FunctionToolInstance> _functionsMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Tools.Tasks _tasksTools;
    private readonly Tools.Git _gitTools;
    private readonly Tools.AzDevOps _azDevOpsTools;
    private readonly Tools.Teams _teamsTools;
    private readonly Tools.FileSystem _fileSystemTools;
    private readonly Tools.Outlook _outlookTools;
    private string _lastDispalayedMessageId = string.Empty;

    public Assistant(
        IDialogPresenter dialogPresenter,
        IOptions<Core.Options.Cognitive> cognitiveOptions, 
        IOptions<Core.Options.Assistant> assistantOptions,
        AndronixTokenCredential andronixTokenCredential,
        Tools.Tasks tasksTools,
        Tools.Git gitTools,
        Tools.AzDevOps azDevOpsTools,
        Tools.Teams teamsTools,
        Tools.FileSystem fileSystemTools,
        Tools.Outlook outlookTools,
        IAuthenticationProvider authenticationProvider) 
    {
        _dialogPresenter = dialogPresenter ?? throw new ArgumentNullException(nameof(dialogPresenter));

        // Options and settings
        _cognitiveOptions = cognitiveOptions.Value ?? throw new ArgumentNullException(nameof(cognitiveOptions));
        _assistantOptions = assistantOptions.Value ?? throw new ArgumentNullException(nameof(assistantOptions));
        _userSettings = new Lazy<Core.UserSettings>(() => 
        {
            // Read from local JSON file
            try
            {
                if (string.IsNullOrWhiteSpace(_assistantOptions.UserSettings)) {
                    _assistantOptions.UserSettings = SpecialPath.UserSettings;
                    if (!Directory.Exists(Path.GetDirectoryName(_assistantOptions.UserSettings))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(_assistantOptions.UserSettings));
                    }
                    return new Core.UserSettings();
                }

                var userSettings = JsonSerializer.Deserialize(File.ReadAllText(_assistantOptions.UserSettings), SourceGenerationContext.Default.UserSettings);
                if (userSettings == null)
                    throw new InvalidOperationException($"Failed to read user settings from {_assistantOptions.UserSettings}");
                return userSettings;
            }
            catch (Exception ex)
            {
                _dialogPresenter.UpdateStatus($"Failed to read user settings from {_assistantOptions.UserSettings}: {ex.Message}");
                throw;
            }
        });

        // Clients
        _azureOpenAIClient = new AzureOpenAIClient(_cognitiveOptions.EndPoint, andronixTokenCredential);
        _assistantClient = _azureOpenAIClient.GetAssistantClient();
        _fileClient = _azureOpenAIClient.GetFileClient();
        _graphClient = new Lazy<GraphServiceClient>(() =>
        {
            var graphClient = new GraphServiceClient(authenticationProvider);
            return graphClient;
        });

        _tasksTools = tasksTools;
        _gitTools = gitTools;
        _azDevOpsTools = azDevOpsTools;
        _teamsTools = teamsTools;
        _fileSystemTools = fileSystemTools;
        _outlookTools = outlookTools;

        InitializeFunctions();
    }

    private void InitializeFunctions()
    {
        InitializeFunctions(this);
        InitializeFunctions(_tasksTools);
        InitializeFunctions(_gitTools);
        InitializeFunctions(_azDevOpsTools);
        InitializeFunctions(_teamsTools);
        InitializeFunctions(_fileSystemTools);
        InitializeFunctions(_outlookTools);
    }

    private void InitializeFunctions(object typeInstance)
    {
        foreach (var method in typeInstance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            var descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute == null)
                continue;

            var parameters = new StringBuilder("{ \"type\": \"object\", \"properties\": {");
            var requiredParameters = new StringBuilder();
            var firstParameter = true;
            foreach (var parameter in method.GetParameters())
            {
                var parameterDescriptionAttribute = parameter.GetCustomAttribute<DescriptionAttribute>();
                if (parameterDescriptionAttribute == null)
                    continue;

                var parameterRequiredAttribute = parameter.GetCustomAttribute<RequiredAttribute>();
                if (parameterRequiredAttribute != null)
                {
                    if (requiredParameters.Length <= 0)
                        requiredParameters.Append("\"required\": [");
                    else
                        requiredParameters.Append(", ");

                    requiredParameters.Append($"\"{parameter.Name}\"");
                }

                if (!firstParameter)
                    parameters.Append(", ");
                else
                    firstParameter = false;

                parameters.Append($"\"{parameter.Name}\": {{ \"type\": \"{parameter.ParameterType.ToJsonType()}\",\"description\":\"{parameterDescriptionAttribute.Description}\"}}");
            }
            parameters.Append($"}}");
            if (0 < requiredParameters.Length)
            {
                requiredParameters.Append("]");
                parameters.Append($", {requiredParameters}");
            }
            parameters.Append($" }}");

            var functionDefinition = new FunctionToolDefinition(method.Name, descriptionAttribute.Description)
            {
                Parameters = BinaryData.FromString(parameters.ToString())
            };

            _functionsMap.Add(method.Name, new FunctionToolInstance
            {
                Name = method.Name,
                Description = descriptionAttribute.Description,
                TypeInstance = typeInstance,
                MethodInfo = method,
                Definition = functionDefinition
            });
        }
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
            catch (ClientResultException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                // Assistant not found
                _userSettings.Value.AssistantId = string.Empty;
            }
        }

        _dialogPresenter.UpdateStatus("Creating assistant...");

        var instructions = new StringBuilder();
        if (string.IsNullOrWhiteSpace(_assistantOptions.Instructions))
            instructions.AppendLine(@"Conversation between the user and the personal assistant.");
        else
            instructions.AppendLine(File.ReadAllText(_assistantOptions.Instructions));

        if (!string.IsNullOrWhiteSpace(_assistantOptions.AboutMe))
            instructions.AppendLine(File.ReadAllText(_assistantOptions.AboutMe));

        IList<string>? assistantFiles = null;
        if (_cognitiveOptions.KnowledgeFiles != null && _cognitiveOptions.KnowledgeFiles.Any())
        {
            var assistantFilesResult = await _fileClient.GetFilesAsync(OpenAIFilePurpose.Assistants);
            assistantFiles = assistantFilesResult.Value
                .Where (x => x.Status == OpenAIFileStatus.Processed)
                .Select(x => x.Id).ToList();
        }
        var creationOptions = new AssistantCreationOptions()
        {
            Name = _assistantOptions.Name,
            Instructions = instructions.ToString(),
            ToolResources = new ()
            {
                FileSearch = assistantFiles != null ? new() : null
            }
        };

        if (assistantFiles != null)
        {
            creationOptions.Tools.Add(ToolDefinition.CreateFileSearch());
            creationOptions.ToolResources.FileSearch!.NewVectorStores.Add(new VectorStoreCreationHelper(assistantFiles));
        }

        AddFunctions(creationOptions.Tools);

        var createResponse = await _assistantClient.CreateAssistantAsync("gpt-4o", creationOptions);
        _userSettings.Value.AssistantId = createResponse.Value.Id;

        // Save settings to file
        using (var fileStream = File.Create(_assistantOptions.UserSettings))
        {
            await JsonSerializer.SerializeAsync(fileStream, _userSettings.Value, SourceGenerationContext.Default.UserSettings);
        }

        _openAiAssistant = createResponse.Value;
    }

    private void AddFunctions(IList<ToolDefinition> tools)
    {
        tools.AddRange(_functionsMap.Values.Select<FunctionToolInstance, ToolDefinition>(x => x.Definition));
    }

    public async Task StartNewThreadAsync()
    {
        if (_openAiAssistant == null)
            throw new InvalidOperationException("Assistant was not created.");

        _dialogPresenter.UpdateStatus("Creating thread...");
        var threadOptions = new ThreadCreationOptions();
        threadOptions.InitialMessages.Add(
            new ThreadInitializationMessage(
                MessageRole.Assistant, 
                [   
                    MessageContent.FromText($"Today is {DateTime.Now:F} {TimeZoneInfo.Local.DisplayName}"),
                    MessageContent.FromText($"It is {DateTimeOffset.Now.ToTimeOfYear()}"),
                    MessageContent.FromText($"It is {DateTimeOffset.Now.ToTimeOfDay()}"),
                    MessageContent.FromText("I am your assistant. I am here ready to help"),
                ])
        );
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
        await _assistantClient.CreateMessageAsync(_openAiAssistantThread, MessageRole.User, [MessageContent.FromText(prompt)]);
        var runResponse = await _assistantClient.CreateRunAsync(_openAiAssistantThread, _openAiAssistant);

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        do
        {
            _dialogPresenter.UpdateStatus($"Waiting for response...{stopWatch.Elapsed:mm\\:ss}");
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            runResponse = await _assistantClient.GetRunAsync(_openAiAssistantThread.Id, runResponse.Value.Id);
            if (runResponse.Value.Status == RunStatus.RequiresAction)
            {
                List<ToolOutput> toolOutputs = [];

                foreach (var action in runResponse.Value.RequiredActions)
                {
                    if (_functionsMap.TryGetValue(action.FunctionName, out var functionInstance))
                    {
                        _dialogPresenter.UpdateStatus($"Executing {action.FunctionName}...");
                        try
                        {
                            var functionOutput = await functionInstance.Invoke(action.FunctionArguments);
                            toolOutputs.Add(new ToolOutput(action.ToolCallId, functionOutput));
                        }
                        catch (Exception ex)
                        {
                            _dialogPresenter.UpdateStatus($"Failed to execute {action.FunctionName}...");
                            toolOutputs.Add(new ToolOutput(action.ToolCallId, $"Faile to execute tool '{action.FunctionName}' with error message: {ex.Message}"));
                        }
                    }
                }

                // Submit the tool outputs to the assistant, which returns the run to the queued state.
                runResponse = _assistantClient.SubmitToolOutputsToRun(_openAiAssistantThread.Id, runResponse.Value.Id, toolOutputs);
            }
        }
        while (!runResponse.Value.Status.IsTerminal);

        var afterRunMessagesResponse = _assistantClient.GetMessages(_openAiAssistantThread.Id, OpenAI.ListOrder.OldestFirst);
        var dialogHtml = new StringBuilder();

        _dialogPresenter.UpdateStatus("Displaying response...");
        bool skipDisplay = true;
        foreach (var threadMessage in afterRunMessagesResponse)
        {
            // Skip assistant messages without run id (initial messages)
            if (threadMessage.Role == MessageRole.Assistant && threadMessage.RunId == null)
                continue;

            if (skipDisplay && !string.IsNullOrWhiteSpace(_lastDispalayedMessageId))
            {
                if (threadMessage.Id == _lastDispalayedMessageId)
                    skipDisplay = false;
                continue;
            }

            Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
            foreach (MessageContent contentItem in threadMessage.Content)
            {
                if (!string.IsNullOrWhiteSpace(contentItem.Text))
                {
                    if (threadMessage.Role == MessageRole.Assistant)
                        dialogHtml.Append($"<div style='color: MediumSeaGreen;'>Assistant: {Markdown.ToHtml(contentItem.Text)}</div>");
                }
            }
        }

        _lastDispalayedMessageId = afterRunMessagesResponse.Last().Id;

        _dialogPresenter.ShowDialog(dialogHtml.ToString());
        _dialogPresenter.UpdateStatus("Ready");
    }

    #endregion

    #region AI Functions


    #endregion

    public async Task GetMeAsync()
    {
        var user = await _graphClient.Value.Me.GetAsync();
        if (user == null)
            throw new InvalidOperationException("Failed to get user details.");
    }
}

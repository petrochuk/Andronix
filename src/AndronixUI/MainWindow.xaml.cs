using Andronix.AssistantAI;
using Andronix.Core;
using Azure.AI.OpenAI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Text;
using Windows.System;

namespace Andronix.UI;

/// <summary>
/// Main Window of the application
/// </summary>
public sealed partial class MainWindow : Window
{
    AssistantAIClient _assistantAIClient;
    ChatCompletionsOptions _chatCompletionsOptions = new();

    public MainWindow()
    {
        InitializeComponent();

        _assistantAIClient = App.ServiceProvider.GetRequiredService<AssistantAIClient>();
        _chatCompletionsOptions = new ChatCompletionsOptions()
        {
            DeploymentName = "gpt-4o",
            ChoiceCount = 1,
            Temperature = (float)0,
            MaxTokens = 4096,
            NucleusSamplingFactor = (float)0.1,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        };

        AppWindow.SetIcon("assistant.ico");

        var cognitiveOptions = App.ServiceProvider.GetRequiredService<IOptions<CognitiveOptions>>();

        if (string.IsNullOrWhiteSpace(cognitiveOptions.Value.SystemMessage))
            _chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage(
            @"You are a personal assistant. Your goal is to assist with saving time. 
                Please, use strategic thinking to provide only truly important information which is relevant to make best possible decisions.
                Only use facts, avoid opinions unless asked, and avoid stating obvious things. If you are not sure ask clarifying questions. 
                Here is some useful knowledge:"));
        else
            _chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage(File.ReadAllText(cognitiveOptions.Value.SystemMessage)));

        if (cognitiveOptions.Value.KnowledgeFiles != null)
        {
            foreach (var file in cognitiveOptions.Value.KnowledgeFiles)
            {
                var systemMessage = new ChatRequestSystemMessage(File.ReadAllText(file));
                _chatCompletionsOptions.Messages.Add(systemMessage);
            }
        }
    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        await _responseView.EnsureCoreWebView2Async();
        _promptText.Focus(FocusState.Programmatic);
    }

    private async void GoButton_Click(object sender, RoutedEventArgs e)
    {
        // If the prompt text is empty, do nothing
        if (string.IsNullOrWhiteSpace(_promptText.Text))
            return;

        _chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(_promptText.Text));
        var response = await _assistantAIClient.GetChatCompletionsAsync(_chatCompletionsOptions);
        _promptText.Text = "";

        foreach (var message in response.Value.Choices)
        {
            _chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(message.Message.Content));
        }

        var dialogHtml = new StringBuilder();
        dialogHtml.Append("<html><body style='font-family: Consolas; font-size: 14px;'>");
        foreach (var message in _chatCompletionsOptions.Messages)
        {
            switch (message)
            {
                case ChatRequestUserMessage userMessage:
                    dialogHtml.Append($"<div style='color: blue;'>{userMessage.Content}</div>");
                    break;
                case ChatRequestAssistantMessage assistantMessage:
                    dialogHtml.Append($"<div style='color: green;'>Assistant: {assistantMessage.Content.Replace("\n", "<br />")}</div>");
                    break;
            }
        }
        _responseView.NavigateToString(dialogHtml.ToString());
    }

    private void PromptText_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            // Handle the "Enter" key to prevent the default behavior of TextBox
            //    which is to insert a new line.
            // If the "Shift" key is pressed, then allow the default behavior.
            var keyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            if ((keyState & Windows.UI.Core.CoreVirtualKeyStates.Down) != Windows.UI.Core.CoreVirtualKeyStates.Down)
            {
                e.Handled = true;
                GoButton_Click(sender, e);
            }
        }
    }
}

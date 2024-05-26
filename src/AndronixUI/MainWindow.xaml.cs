using Andronix.OpenAI;
using Azure.AI.OpenAI;
using Microsoft.UI.Xaml;
using System;

namespace Andronix.UI;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    AssistantAIClient _assistantAIClient = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        await _responseView.EnsureCoreWebView2Async();
    }

    private async void myButton_Click(object sender, RoutedEventArgs e)
    {
        var response = await _assistantAIClient.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                DeploymentName = "gpt-4o",
                Messages =
                {
                    new ChatRequestSystemMessage(@"You are an AI assistant")
                },
                Temperature = (float)1,
                MaxTokens = 4096,


                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            }
        );

        _responseView.NavigateToString(response.Value.Choices[0].Message.Content);
    }
}

using Andronix.AssistantAI;
using Andronix.Interfaces;
using Azure.AI.OpenAI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Andronix.UI;

/// <summary>
/// Main Window of the application
/// </summary>
public sealed partial class MainWindow : Window, IDialogPresenter
{
    Assistant? _assistant;
    IBackgroundTaskQueue? _assistantTaskQueue;
    ChatCompletionsOptions _chatCompletionsOptions = new();

    public MainWindow()
    {
        InitializeComponent();

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
    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_assistant == null)
        {
            _assistant = Program.Host.Services.GetRequiredService<Assistant>();
            _assistantTaskQueue = Program.Host.Services.GetRequiredService<IBackgroundTaskQueue>();
            await _assistantTaskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
            {
                await _assistant.CreateAssistantAsync();
                await _assistant.StartNewThreadAsync();
            });
        }

        await _responseView.EnsureCoreWebView2Async();
        _promptText.Focus(FocusState.Programmatic);
    }

    private async void GoButton_Click(object sender, RoutedEventArgs e)
    {
        // If the prompt text is empty, do nothing
        if (string.IsNullOrWhiteSpace(_promptText.Text))
            return;

        var promptText = _promptText.Text;
        await _assistantTaskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
        {
            await _assistant.SendPrompt(promptText);
        });
        _promptText.Text = "";
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

    #region IDialogPresenter

    public void ShowDialog(string fullDialog)
    {
        bool isQueued = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            _responseView.NavigateToString(fullDialog);
        });
    }

    #endregion
}

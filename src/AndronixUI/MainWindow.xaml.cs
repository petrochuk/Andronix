using Andronix.AssistantAI;
using Andronix.Interfaces;
using Markdig;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Text;
using System.Web;
using Windows.System;

namespace Andronix.UI;

/// <summary>
/// Main Window of the application
/// </summary>
public sealed partial class MainWindow : Window, IDialogPresenter
{
    Assistant? _assistant;
    IBackgroundTaskQueue? _assistantTaskQueue;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon("assistant.ico");
        _promptText.SuggestionsSource = 
        [
            "detail",
            "eighth",
            "eleventh",
            "feature",
            "fifth",
            "first",
            "folder",
            "fourth",
            "ninth",
            "second",
            "seventh",
            "sixth",
            "task",
            "tenth",
            "third",
            "twelfth",
        ];

        // Set the default background color to theme color
        _responseView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_assistant == null)
        {
            _statusText.Text = "Initializing...";
            _assistant = Program.Host.Services.GetRequiredService<Assistant>();
            _assistantTaskQueue = Program.Host.Services.GetRequiredService<IBackgroundTaskQueue>();
            await _assistantTaskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
            {
                await _assistant.CreateAssistantAsync();
                await _assistant.StartNewThreadAsync();
            });
            await _responseView.EnsureCoreWebView2Async();
            _responseView.NavigateToString(
@"<html>
    <style>
        a:link, a:visited, a:hover, a:active 
        {
            color: DeepSkyBlue; 
            background-color: transparent;
        }
    </style>
    <body style='font-family: Consolas; font-size: 14px;'>
    </body>
</html>
");

            await _assistantTaskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
            {
                await _assistant.SendPrompt("hi, what are my next tasks?");
            });
        }

        _promptText.Focus(FocusState.Programmatic);
    }

    private async void GoButton_Click(object sender, RoutedEventArgs e)
    {
        // If the prompt text is empty, do nothing
        if (string.IsNullOrWhiteSpace(_promptText.Text))
            return;

        var promptText = _promptText.Text;
        var promptHtml = new StringBuilder();
        promptHtml.Append($"<div style='color: white;'>{Markdown.ToHtml(promptText)}</div>");
        ShowDialog(promptHtml.ToString());
        _promptText.Text = "";

        await _assistantTaskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
        {
            await _assistant.SendPrompt(promptText);
        });
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

    private async void ResponseView_NavigationStarting(Microsoft.UI.Xaml.Controls.WebView2 sender, 
        Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        var uri = new Uri(args.Uri);
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return;

        args.Cancel = true;
        _ = await Launcher.LaunchUriAsync(uri);
    }

    #region IDialogPresenter

    public void ShowDialog(string fullDialog)
    {
        if (DispatcherQueue == null)
            return;

        bool isQueued = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
        {
            var result = await _responseView.ExecuteScriptAsync($"document.body.insertAdjacentHTML('beforeend', '<p>{HttpUtility.JavaScriptStringEncode(fullDialog)}</p>');");
        });
    }

    public void UpdateStatus(string status)
    {
        if (DispatcherQueue == null)
            return;

        bool isQueued = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            _statusText.Text = status;
        });
    }

    #endregion

}

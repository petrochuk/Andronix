using Andronix.AssistantAI;
using Andronix.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Andronix.UI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application, IApplication, IActionApprover
{
    private Window _window;
    private TeamsKnowledgeCollector _intelligenceGatherer;
    private TeamsAssistant _teamsAssistant;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    #region IAppHost

    public IntPtr GetMainWindowHandle()
    {
        return WinRT.Interop.WindowNative.GetWindowHandle(_window);
    }

    public string Title => "Andronix";

    #endregion

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (_window != null)
            return;

        _window = Program.Host.Services.GetRequiredService<MainWindow>();
        _intelligenceGatherer = Program.Host.Services.GetRequiredService<TeamsKnowledgeCollector>();
        _teamsAssistant = Program.Host.Services.GetRequiredService<TeamsAssistant>();
#if DEBUG
        //_intelligenceGatherer.Start();
        //_teamsAssistant.Start();
#endif

        // Maximize
        if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
        _window.Title = Title;
        _window.Activate();
    }

    public void OnStopApplication()
    {
        _intelligenceGatherer.Stop();
        _teamsAssistant.Stop();
    }

    private static IConfiguration LoadConfiguration()
    {
        var userAppSettings = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Andronix", "appSettings.json");
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(userAppSettings, optional: true, reloadOnChange: true);

#if DEBUG
        builder.AddJsonFile("appSettings.Debug.json", optional: true, reloadOnChange: true);
#endif

        return builder.Build();
    }

    public Task<(bool isApproved, string declineReason)> ApproveAction(string action)
    {
        var result = new TaskCompletionSource<(bool, string)>();

        _window.DispatcherQueue.TryEnqueue(async () =>
        {
            var notifyDialog = new ContentDialog
            {
                XamlRoot = _window.Content.XamlRoot,
                Title = "Review Action",
                Content = action,
                PrimaryButtonText = "Approve",
                SecondaryButtonText = "Decline"
            };

            var notifyResult = await notifyDialog.ShowAsync();

            result.SetResult((notifyResult == ContentDialogResult.Primary, string.Empty));
        });

        return result.Task;
    }
}

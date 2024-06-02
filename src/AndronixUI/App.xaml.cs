using Andronix.AssistantAI;
using Andronix.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.IO;
using System.Reflection;

namespace Andronix.UI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application, IApplication
{
    private Window _window;
    private TeamsKnowledgeCollector _intelligenceGatherer;

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
        _window = Program.Host.Services.GetRequiredService<MainWindow>();
        _intelligenceGatherer = Program.Host.Services.GetRequiredService<TeamsKnowledgeCollector>();
#if DEBUG
        _intelligenceGatherer.Start();
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
}

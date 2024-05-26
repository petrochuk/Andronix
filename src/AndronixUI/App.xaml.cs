using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace Andronix.UI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();

        // Maximize
        if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
        _window.Activate();
    }

    private Window _window;
}

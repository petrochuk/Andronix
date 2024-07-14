using Microsoft.UI.Xaml;

namespace Andronix.UI.Dialogs;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ActionConfirmation : Window
{
    public ActionConfirmation()
    {
        this.InitializeComponent();
    }

    private void Window_Closed(object sender, WindowEventArgs e)
    {
        this.Close();
        DispatcherQueue.EnqueueEventLoopExit();
    }
}

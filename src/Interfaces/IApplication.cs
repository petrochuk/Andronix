namespace Andronix.Interfaces;

public interface IApplication
{
    IntPtr GetMainWindowHandle();

    string Title { get; }

    void OnStopApplication();
}
